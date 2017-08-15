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
        ComponentDB _dbCurrent;
        DirectoryStreamStore _store;

        /// <summary>
        /// Initializes a new <see cref="CKSetupStoreMiddleware"/>.
        /// </summary>
        /// <param name="next">Next middleware.</param>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="options">Middleware options.</param>
        public CKSetupStoreMiddleware( RequestDelegate next, IActivityMonitor monitor, CKSetupStoreMiddlewareOptions options, IMemoryCache cache )
        {
            _next = next;
            _cache = cache;
            _store = new DirectoryStreamStore( options.RootStorePath );
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
                ctx.Response.SetNoCacheAndDefaultStatus( StatusCodes.Status404NotFound );
                if( remainder.Value == ClientRemoteStore.PushPath )
                {
                    if( HttpMethods.IsPost( ctx.Request.Method ) ) return HandlePush( ctx, ctx.GetRequestMonitor() );
                    ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
                else if( remainder.StartsWithSegments( ClientRemoteStore.PushFilePath, out var sha ) )
                {
                    if( HttpMethods.IsPost( ctx.Request.Method ) ) return HandlePushFile( ctx, ctx.GetRequestMonitor(), sha );
                    ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
            return _next.Invoke( ctx );
        }

        async Task HandlePush( HttpContext ctx, IActivityMonitor monitor )
        {
            var apiKey = (string)ctx.Request.Headers[ClientRemoteStore.ApiKeyHeader];
            if( apiKey != "HappyKey" )
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
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
                        cacheEntry.SetSlidingExpiration( TimeSpan.FromSeconds( 500 ) );
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
            if( initial == null || !SHA1Value.TryParse( shaPath.Value, 1, out var sha1 ) )
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
                    monitor.Error( $"Temporary file SHA is {localSha} but should be {sha1}." );
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
    }
}
