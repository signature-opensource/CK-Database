using CK.Core;
using CKSetup;
using CKSetup.StreamStore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetupRemoteStore
{
    /// <summary>
    /// Manages a <see cref="DirectoryStreamStore"/> and exposes its <see cref="ComponentDB"/> along
    /// with other services that require synchronization.
    /// This must be registered as a singleton service.
    /// </summary>
    public class ComponentDBProvider
    {
        readonly IMemoryCache _cache;
        readonly ReaderWriterLockSlim _rwLock;
        readonly TimeSpan _pushSessionDuration;
        readonly DirectoryStreamStore _store;
        ComponentDB _dbCurrent;
        ComponentDBInfo _dbInfo;
        string _xmlDb;

        public ComponentDBProvider(
            IHostingEnvironment env,
            IMemoryCache cache,
            IOptions<CKSetupStoreOptions> options )
        {
            CKSetupStoreOptions opt = options.Value;
            _pushSessionDuration = opt.PushSessionDuration;
            string storePath = opt.RootStorePath;
            if( String.IsNullOrWhiteSpace( storePath ) ) storePath = "Store";
            if( !Path.IsPathRooted( storePath ) )
            {
                storePath = Path.Combine( env.ContentRootPath, storePath );
            }
            _cache = cache;
            _store = new DirectoryStreamStore( storePath );
            var monitor = new ActivityMonitor( "Initializing ComponentDB from store." );
            monitor.Info( $"Store path: {storePath}" );
            _dbCurrent = _store.Initialize( monitor );
            if( _dbCurrent == null ) throw new Exception( "Fatal error while initializing store." );
            _rwLock = new ReaderWriterLockSlim( LockRecursionPolicy.NoRecursion );
        }


        /// <summary>
        /// Gets the current statistics about components.
        /// </summary>
        public ComponentDBInfo Info
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    if( _dbInfo == null )
                    {
                        _dbInfo = new ComponentDBInfo( _dbCurrent );
                    }
                    return _dbInfo;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the current component database.
        /// </summary>
        public ComponentDB ComponentDB => _dbCurrent;

        /// <summary>
        /// Gets the current component database as a xml string.
        /// </summary>
        public string ComponentDBAsXmlString
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    if( _xmlDb == null )
                    {
                        _xmlDb = new XDocument( _dbCurrent.ToXml() ).ToString();
                    }
                    return _xmlDb;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Writes a zip file with all the files for a set of components.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="components">Set of components.</param>
        /// <param name="output">Output stream.</param>
        /// <returns>The continuation.</returns>
        public async Task ExportZippedComponentFiles( IActivityMonitor monitor, IEnumerable<Component> components, Stream output )
        {
            HashSet<string> dedup = new HashSet<string>();
            using( ZipArchive a = new ZipArchive( output, ZipArchiveMode.Create, true ) )
            {
                foreach( var f in components.SelectMany( c => c.Files ) )
                {
                    if( dedup.Add( f.Name ) )
                    {
                        var e = a.CreateEntry( f.Name );
                        using( var content = e.Open() )
                        {
                            using( var file = _store.OpenUncompressedRead( f.SHA1.ToString() ) )
                            {
                                await file.CopyToAsync( content );
                            }
                        }
                    }
                }
            }
            monitor.Info( $"Exported {dedup.Count} files in zip." );
        }

        /// <summary>
        /// Writes a file (GZip compressed) content.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="sha1">The SHA1 of the file.</param>
        /// <param name="output">The output stream.</param>
        /// <returns>True if the file has been found, false otherwise.</returns>
        public async Task<bool> ExportFile( IActivityMonitor monitor, SHA1Value sha1, Stream output )
        {
            var s = _store.OpenRead( sha1.ToString(), CompressionKind.GZiped );
            if( s.Stream == null )
            {
                return false;
            }
            await s.Stream.CopyToAsync( output );
            return true;
        }

        /// <summary>
        /// Imports a set of components from an input stream (binary protocol) and
        /// returns a <see cref="PushComponentsResult"/> that describes an error or
        /// exposes a seesion identifier and a list of files that should be pushed to this
        /// store since they are not yet available.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="input">The input.</param>
        /// <returns>The import result.</returns>
        public PushComponentsResult ImportComponents( IActivityMonitor monitor, Stream input )
        {
            PushComponentsResult result;
            using( monitor.OpenInfo( "Starting import." ) )
            {
                _rwLock.EnterUpgradeableReadLock();
                try
                {
                    var n = _dbCurrent.Import( monitor, input );
                    if( n.Error ) return new PushComponentsResult( "Error while importing component into ComponentDB.", null );

                    var missingFiles = n.Components
                                        .Where( c => c.StoreFiles )
                                        .SelectMany( c => c.Files )
                                        .Select( f => f.SHA1 )
                                        .Distinct()
                                        .Where( sha => !_store.Exists( sha.ToString() ) )
                                        .ToList();

                    string sessionId = null;
                    if( missingFiles.Count > 0 )
                    {
                        sessionId = Guid.NewGuid().ToString();
                        monitor.Info( $"New session: {sessionId}. Expecting {missingFiles.Count} file(s)." );
                    }
                    result = new PushComponentsResult( missingFiles, sessionId );
                    if( missingFiles.Count > 0 )
                    {
                        using( var cacheEntry = _cache.CreateEntry( sessionId ) )
                        {
                            cacheEntry.SetSlidingExpiration( _pushSessionDuration );
                            cacheEntry.Priority = CacheItemPriority.NeverRemove;
                            cacheEntry.SetValue( result );
                        }
                        Debug.Assert( _cache.Get<PushComponentsResult>( sessionId ) == result );
                    }
                    if( _dbCurrent != n.NewDB )
                    {
                        using( monitor.OpenInfo( $"Saving new database ({n.NewDB.Components.Count} components)." ) )
                        {
                            _rwLock.EnterWriteLock();
                            try
                            {
                                _dbCurrent = n.NewDB;
                                _dbInfo = null;
                                _xmlDb = null;
                                _store.Save( _dbCurrent );
                            }
                            catch( Exception ex )
                            {
                                monitor.Error( ex );
                            }
                            finally
                            {
                                _rwLock.ExitWriteLock();
                            }
                        }
                    }
                    return result;
                }
                catch( Exception ex )
                {
                    monitor.Error( ex );
                    return new PushComponentsResult( ex.Message, null );
                }
                finally
                {
                    _rwLock.ExitUpgradeableReadLock();
                }
            }
        }

        /// <summary>
        /// Validates a sha1 path: it must be a valid SHA1 registered in the session.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="shaPath">The sha1 as a path.</param>
        /// <param name="sessionId">The import session identifier.</param>
        /// <returns>A valid SHA1 or <see cref="SHA1Value.ZeroSHA1"/> when invalid.</returns>
        public SHA1Value ValidPushFileRequest( IActivityMonitor monitor, PathString shaPath, string sessionId )
        {
            var initial = _cache.Get<PushComponentsResult>( sessionId );
            SHA1Value sha1;
            if( initial == null )
            {
                monitor.Error( "Unknown session identifier." );
            }
            else if( !SHA1Value.TryParse( shaPath.Value, 1, out sha1 ) )
            {
                monitor.Error( "Invalid SHA1." );
            }
            else if( !initial.Files.Contains( sha1 ) )
            {
                monitor.Error( $"SHA1 file '{sha1}' does not belong to the import session." );
                sha1 = SHA1Value.ZeroSHA1;
            }
            return sha1;
        }

        /// <summary>
        /// Handles the push of a file.
        /// The <paramref name="sha1"/> must first have been validated by <see cref="ValidPushFileRequest"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="sha1">The sha1 of the file.</param>
        /// <param name="bodyStream">The content stream (GZip compressed).</param>
        /// <returns>The Http <see cref="StatusCodes"/> can be 200 OK, 400 Bad Request or 500 Internal Server error.</returns>
        public async Task<int> HandlePushFileAsync( IActivityMonitor monitor, SHA1Value sha1, Stream bodyStream )
        {
            string targetFileName = _store.GetFullPath( CompressionKind.GZiped, sha1.ToString() );
            if( TargetFileExists( monitor, sha1, targetFileName ) ) return StatusCodes.Status200OK;
            using( var temp = new TemporaryFile() )
            {
                monitor.Debug( $"Accepting file content in temporary: {temp.Path}" );
                using( var output = new FileStream( temp.Path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous ) )
                {
                    await bodyStream.CopyToAsync( output );
                }
                // Temporary: should be done with a Tee stream and the SHA1Stream during the copy above.
                var localSha = await SHA1Value.ComputeFileSHA1Async( temp.Path, r => new GZipStream( r, CompressionMode.Decompress, true ) );
                if( localSha != sha1 )
                {
                    monitor.Error( $"Temporary file '{temp.Path}' SHA is {localSha} but should be {sha1}. Uploaded file kept for possible analysis." );
                    temp.Detach();
                    return StatusCodes.Status400BadRequest;
                }
                if( TargetFileExists( monitor, sha1, targetFileName ) ) return StatusCodes.Status200OK;
                const int maxRetryCount = 5;
                TimeSpan retryTime = TimeSpan.FromMilliseconds( 200 );

                int retryCount = 0;
                tryAgain:
                try
                {
                    File.Move( temp.Path, targetFileName );
                    if( retryCount > 0 ) monitor.Warn( $"Successful file move required {retryCount} try(ies)." );
                }
                catch( Exception ex )
                {
                    if( !TargetFileExists( monitor, sha1, targetFileName ) )
                    {
                        monitor.Error( ex );
                        if( ++retryCount <= maxRetryCount )
                        {
                            monitor.Info( $"Waiting {retryTime}. (retryCount = {retryCount})." );
                            await Task.Delay( retryTime );
                            goto tryAgain;
                        }
                        monitor.Error( $"Failed to move file '{temp.Path}' to '{targetFileName}'." );
                        return StatusCodes.Status500InternalServerError;
                    }
                    else monitor.Warn( "Concurency upload clash. File has already been uploaded.", ex );
                }
            }
            return StatusCodes.Status200OK;
        }

        static bool TargetFileExists( IActivityMonitor monitor, SHA1Value sha1, string targetFileName )
        {
            if( File.Exists( targetFileName ) )
            {
                monitor.Warn( $"File {sha1} already exists." );
                return true;
            }
            return false;
        }
    }
}
