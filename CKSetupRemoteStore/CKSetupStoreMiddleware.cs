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
        ComponentDB _dbCurrent;
        DirectoryStreamStore _store;

        /// <summary>
        /// Initializes a new <see cref="CKSetupStoreMiddleware"/>.
        /// </summary>
        /// <param name="next">Next middleware.</param>
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
            if( ctx.Request.Path.StartsWithSegments( _root, out remainder ) )
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
                                        .Where( c => c.ComponentKind != ComponentKind.Model )
                                        .SelectMany( c => c.Files )
                                        .Select( f => f.SHA1 )
                                        .Distinct()
                                        .Where( sha => !_store.Exists( sha.ToString() ) )
                                        .ToList();

                    string sessionId = Guid.NewGuid().ToString();
                    monitor.Info( $"New session: {sessionId}" );
                    result = new PushComponentsResult( missingFiles, sessionId );
                    using( var cacheEntry = _cache.CreateEntry( sessionId ) )
                    {
                        cacheEntry.SetSlidingExpiration( _pushSessionDuration );
                        cacheEntry.Priority = CacheItemPriority.NeverRemove;
                        cacheEntry.SetValue( result );
                    }
                    Debug.Assert( _cache.Get<PushComponentsResult>( sessionId ) == result );
                    if( _dbCurrent != n.NewDB )
                    {
                        _rwLock.EnterWriteLock();
                        try
                        {
                            _dbCurrent = n.NewDB;
                            _store.Save( _dbCurrent );
                        }
                        finally
                        {
                            _rwLock.ExitWriteLock();
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
            if( initial == null 
                || !SHA1Value.TryParse( shaPath.Value, 1, out var sha1 )
                || !initial.Files.Contains( sha1 ) )
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
