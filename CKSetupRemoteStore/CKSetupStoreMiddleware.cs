using CK.AspNet;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CK.Core;
using CKSetup;
using CKSetup.StreamStore;
using System.Threading;
using System.IO;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;

namespace CKSetupRemoteStore
{
    /// <summary>
    /// Handles requests to /.cksetup/store.
    /// </summary>
    public class CKSetupStoreMiddleware
    {
        static readonly PathString _root = new PathString( ClientRemoteStore.RootPathString );

        readonly RequestDelegate _next;
        readonly IMemoryCache _cache;
        readonly ReaderWriterLockSlim _rwLock;
        readonly TimeSpan _pushSessionDuration;
        readonly HashSet<string> _apiKeys;
        readonly DirectoryStreamStore _store;
        readonly PathString _dlZipPrefix;
        readonly PathString _componentInfoPrefix;
        ComponentDB _dbCurrent;

        /// <summary>
        /// Initializes a new <see cref="CKSetupStoreMiddleware"/>.
        /// </summary>
        /// <param name="next">Next middleware.</param>
        /// <param name="env">Hosting environment.</param>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="options">Middleware options.</param>
        public CKSetupStoreMiddleware( RequestDelegate next, IActivityMonitor monitor, IHostingEnvironment env, IOptions<CKSetupStoreMiddlewareOptions> options, IMemoryCache cache )
        {
            CKSetupStoreMiddlewareOptions opt = options.Value;
            if( opt.ApiKeys == null 
                || (_apiKeys = new HashSet<string>( opt.ApiKeys.Where( key => !string.IsNullOrWhiteSpace( key ) ) )).Count == 0 )
            {
                throw new ArgumentException( "There must be at least one non empty string key.", nameof( opt.ApiKeys ) );
            }
            _pushSessionDuration = opt.PushSessionDuration;
            _dlZipPrefix = opt.DownloadZipPrefix;
            if( !_dlZipPrefix.HasValue ) _dlZipPrefix = "/dl-zip";
            _componentInfoPrefix = opt.ComponentInfoPrefix;
            if( !_componentInfoPrefix.HasValue ) _componentInfoPrefix = "/component-info";
            string storePath = opt.RootStorePath;
            if( String.IsNullOrWhiteSpace( storePath ) ) storePath = "Store";
            if( !Path.IsPathRooted( storePath ) )
            {
                storePath = Path.Combine( env.ContentRootPath, storePath );
            }
            monitor.Info( $"Store path: {storePath}" );
            _next = next;
            _cache = cache;
            _store = new DirectoryStreamStore( storePath );
            _dbCurrent = _store.Initialize( monitor );
            _rwLock = new ReaderWriterLockSlim( LockRecursionPolicy.NoRecursion );
        }

        /// <summary>
        /// </summary>
        /// <param name="ctx">The current context.</param>
        /// <returns>The awaitable.</returns>
        public Task Invoke( HttpContext ctx )
        {
            PathString remainder;
            if( ctx.Request.Path.StartsWithSegments( _dlZipPrefix, out remainder ) )
            {
                ctx.Response.SetNoCacheAndDefaultStatus( StatusCodes.Status404NotFound );
                if( HttpMethods.IsGet( ctx.Request.Method ) ) return HandleDownloadZip( ctx, remainder, ctx.GetRequestMonitor() );
                ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            }
            else if( ctx.Request.Path.StartsWithSegments( _componentInfoPrefix, out remainder ) )
            {
                ctx.Response.SetNoCacheAndDefaultStatus( StatusCodes.Status404NotFound );
                if( HttpMethods.IsGet( ctx.Request.Method ) ) return HandleComponentInfo( ctx, remainder, ctx.GetRequestMonitor() );
                ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            }
            else if( ctx.Request.Path.StartsWithSegments( _root, out remainder ) )
            {
                PathString sha;
                ctx.Response.SetNoCacheAndDefaultStatus( StatusCodes.Status404NotFound );
                if( remainder.Value == ClientRemoteStore.PullPath )
                {
                    if( HttpMethods.IsPost( ctx.Request.Method ) ) return HandlePull( ctx, ctx.GetRequestMonitor() );
                    ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
                if( remainder.StartsWithSegments( ClientRemoteStore.PullFilePath, out sha ) )
                {
                    if( HttpMethods.IsGet( ctx.Request.Method ) ) return HandlePullFile( ctx, ctx.GetRequestMonitor(), sha );
                    ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
                else if( remainder.Value == ClientRemoteStore.PushPath )
                {
                    if( HttpMethods.IsPost( ctx.Request.Method ) ) return HandlePush( ctx, ctx.GetRequestMonitor() );
                    ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
                else if( remainder.StartsWithSegments( ClientRemoteStore.PushFilePath, out sha ) )
                {
                    if( HttpMethods.IsPost( ctx.Request.Method ) ) return HandlePushFile( ctx, ctx.GetRequestMonitor(), sha );
                    ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
            return _next.Invoke( ctx );
        }

        Task HandleComponentInfo( HttpContext ctx, PathString remainder, IActivityMonitor monitor )
        {
            var req = GetRequestParameterParseResult<TargetFramework>.Parse( remainder );
            if( req.ErrorMessage != null )
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                ctx.Response.Headers.Add( "ErrorMsg", req.ErrorMessage );
                return Task.CompletedTask;
            }
            Component found;
            if( req.Version != null )
            {
                found = _dbCurrent.Components.FirstOrDefault( c => c.Name == req.Name && c.TargetFramework == req.Target && c.Version == req.Version );
            }
            else
            {
                found = _dbCurrent.Components.Where( c => c.Name == req.Name && c.TargetFramework == req.Target )
                            .OrderByDescending( c => c.Version )
                            .FirstOrDefault();
            }
            if( found == null )
            {
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            }
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.GetTypedHeaders().ContentType = new MediaTypeHeaderValue( "application/xml" );
            return ctx.Response.WriteAsync( new XDocument( found.ToXml() ).ToString() );
        }

        async Task HandleDownloadZip( HttpContext ctx, PathString remainder, IActivityMonitor monitor )
        {
            var req = GetRequestParameterParseResult<TargetRuntime>.Parse( remainder );
            if( req.ErrorMessage != null )
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                ctx.Response.Headers.Add( "ErrorMsg", req.ErrorMessage );
                return;
            }
            IReadOnlyList<Component> components = _dbCurrent.ResolveLocalDependencies( monitor, req.Name, req.Target, req.Version );
            if( components == null )
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            ctx.Response.StatusCode = StatusCodes.Status200OK;
            using( ZipArchive a = new ZipArchive( ctx.Response.Body, ZipArchiveMode.Create, true ) )
            {
                foreach( var f in components.SelectMany( c => c.Files ) )
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

        #region Pull

        async Task HandlePull( HttpContext ctx, IActivityMonitor monitor )
        {
            using( var buffer = new MemoryStream() )
            {
                await ctx.Request.Body.CopyToAsync( buffer );
                buffer.Position = 0;
                var missing = new ComponentMissingDescription( XElement.Load( buffer ) );

                ComponentDB db = _dbCurrent;
                var toExport = db.FindAvailable( missing, monitor );
                buffer.Position = 0;
                db.Export( c => toExport.Contains( c ), buffer );
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                await ctx.Response.Body.WriteAsync( buffer.GetBuffer(), 0, (int)buffer.Position );
            }
        }

        async Task HandlePullFile( HttpContext ctx, IActivityMonitor monitor, PathString shaPath )
        {
            if( !SHA1Value.TryParse( shaPath.Value, 1, out var sha1 ) )
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            var s = _store.OpenRead( sha1.ToString(), CompressionKind.GZiped );
            if( s.Stream == null )
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            await s.Stream.CopyToAsync( ctx.Response.Body );
        }

        #endregion

        #region Push (HandlePush, ImportComponent, HandlePushFile)

        async Task HandlePush( HttpContext ctx, IActivityMonitor monitor )
        {
            var apiKey = (string)ctx.Request.Headers[ClientRemoteStore.ApiKeyHeader];
            if( !_apiKeys.Contains( apiKey ) )
            {
                monitor.Warn( "Bad API key." );
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            monitor.Info( $"Pushing with Api key: {apiKey}" );
            using( var buffer = new MemoryStream() )
            {
                await ctx.Request.Body.CopyToAsync( buffer );
                buffer.Position = 0;
                var result = ImportComponents( monitor, buffer );

                ctx.Response.StatusCode = result.ErrorText != null 
                                               ? StatusCodes.Status500InternalServerError 
                                               : StatusCodes.Status200OK;
                buffer.Position = 0;
                result.Write( new CKBinaryWriter( buffer, Encoding.UTF8, true ) );
                await ctx.Response.Body.WriteAsync( buffer.GetBuffer(), 0, (int)buffer.Position );
            }
        }

        PushComponentsResult ImportComponents( IActivityMonitor monitor, Stream input )
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

        async Task HandlePushFile( HttpContext ctx, IActivityMonitor monitor, PathString shaPath )
        {
            var sessionId = (string)ctx.Request.Headers[ClientRemoteStore.SessionIdHeader];
            var initial = _cache.Get<PushComponentsResult>( sessionId );
            SHA1Value sha1 = ValidPushFileRequest( monitor, shaPath, initial );
            if( sha1.IsZero )
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            monitor.Info( $"SessionId={sessionId}, SHA1={sha1}." );
            string targetFileName = _store.GetFullPath( CompressionKind.GZiped, sha1.ToString() );
            if( TargetFileExists( ctx, monitor, sha1, targetFileName ) ) return;

            using( var temp = new TemporaryFile() )
            {
                monitor.Debug( $"Accepting file content in temporary: {temp.Path}" );
                using( var output = new FileStream( temp.Path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous ) )
                {
                    await ctx.Request.Body.CopyToAsync( output );
                }
                // Temporary: should be done with a Tee stream and the SHA1Stream during the copy above.
                var localSha = await SHA1Value.ComputeFileSHA1Async( temp.Path, r => new GZipStream( r, CompressionMode.Decompress, true ) );
                if( localSha != sha1 )
                {
                    monitor.Error( $"Temporary file '{temp.Path}' SHA is {localSha} but should be {sha1}. Uploaded file kept for possible analysis." );
                    temp.Detach();
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                if( TargetFileExists( ctx, monitor, sha1, targetFileName ) ) return;
                try
                {
                    File.Move( temp.Path, targetFileName );
                }
                catch( Exception ex )
                {
                    if( !TargetFileExists( ctx, monitor, sha1, targetFileName ) )
                    {
                        monitor.Error( ex );
                        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        return;
                    }
                    else monitor.Warn( "Concurency upload clash. File has already been uploaded.", ex );
                }
            }
            ctx.Response.StatusCode = StatusCodes.Status200OK;
        }

        static SHA1Value ValidPushFileRequest( IActivityMonitor monitor, PathString shaPath, PushComponentsResult initial )
        {
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

        static bool TargetFileExists( HttpContext ctx, IActivityMonitor monitor, SHA1Value sha1, string targetFileName )
        {
            if( File.Exists( targetFileName ) )
            {
                monitor.Warn( $"File {sha1} already exists." );
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return true;
            }
            return false;
        }

        #endregion


    }
}
