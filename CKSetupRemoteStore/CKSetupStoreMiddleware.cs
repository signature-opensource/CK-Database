using CK.AspNet;
using CK.Core;
using CKSetup;
using CKSetup.StreamStore;
using CSemVer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetupRemoteStore
{
    /// <summary>
    /// Handles requests to /.cksetup/store.
    /// </summary>
    public class CKSetupStoreMiddleware
    {
        static readonly PathString _root = new PathString( ClientRemoteStore.RootPathString );

        readonly RequestDelegate _next;
        readonly HashSet<string> _apiKeys;
        readonly PathString _dlZipPrefix;
        readonly PathString _componentInfoPrefix;
        readonly PathString _componentDbPrefix;
        readonly ComponentDBProvider _dbProvider;

        /// <summary>
        /// Initializes a new <see cref="CKSetupStoreMiddleware"/>.
        /// </summary>
        /// <param name="next">Next middleware.</param>
        /// <param name="options">Store options.</param>
        /// <param name="dbProvider">Component database provider.</param>
        public CKSetupStoreMiddleware(
            RequestDelegate next,
            IOptions<CKSetupStoreOptions> options,
            ComponentDBProvider dbProvider )
        {
            CKSetupStoreOptions opt = options.Value;
            if( opt.ApiKeys == null 
                || (_apiKeys = new HashSet<string>( opt.ApiKeys.Where( key => !string.IsNullOrWhiteSpace( key ) ) )).Count == 0 )
            {
                throw new ArgumentException( "There must be at least one non empty string key.", nameof( opt.ApiKeys ) );
            }
            _dlZipPrefix = opt.DownloadZipPrefix;
            if( !_dlZipPrefix.HasValue ) _dlZipPrefix = "/dl-zip";
            _componentInfoPrefix = opt.ComponentInfoPrefix;
            if( !_componentInfoPrefix.HasValue ) _componentInfoPrefix = "/component-info";
            _componentDbPrefix = opt.ComponentDbPrefix;
            if( !_componentDbPrefix.HasValue ) _componentDbPrefix = "/component-db";
            _next = next;
            _dbProvider = dbProvider;
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
            else if( ctx.Request.Path.StartsWithSegments( _componentDbPrefix, out remainder ) )
            {
                ctx.Response.SetNoCacheAndDefaultStatus( StatusCodes.Status404NotFound );
                if( HttpMethods.IsGet( ctx.Request.Method ) ) return HandleComponentDb( ctx, ctx.GetRequestMonitor() );
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

        Task HandleComponentDb( HttpContext ctx, IActivityMonitor monitor )
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.GetTypedHeaders().ContentType = new MediaTypeHeaderValue( "application/xml" );
            return ctx.Response.WriteAsync( _dbProvider.ComponentDBAsXmlString );
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
            var db = _dbProvider.ComponentDB;
            Component found;
            if( req.Version != null )
            {
                found = db.Components.FirstOrDefault( c => c.Name == req.Name
                                                                    && c.TargetFramework == req.Target
                                                                    && (req.Version == null || c.Version == req.Version) );
            }
            else if( req.IsLastVersion )
            {
                found = db.Components.Where( c => c.Name == req.Name && c.TargetFramework == req.Target )
                                             .OrderByDescending( c => c.Version )
                                             .FirstOrDefault();
            }
            else
            {
                Func<SVersion, bool> filter = FilterPreview;
                if( req.VersionMoniker == "release" ) filter = FilterRelease;
                found = db.Components.Where( c => c.Name == req.Name && c.TargetFramework == req.Target )
                            .OrderByDescending( c => c.Version )
                            .Where( c => filter( c.Version ) )
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

        /// <summary>
        /// /dl-zip/ComponentName/RuntimeOrFramework/Version where version is optional 
        /// </summary>
        /// <param name="ctx">The current http context.</param>
        /// <param name="remainder">The ComponentName/RuntimeOrFramework/Version part.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>The continuation.</returns>
        Task HandleDownloadZip( HttpContext ctx, PathString remainder, IActivityMonitor monitor )
        {
            var req = GetRequestParameterParseResult<TargetRuntime>.Parse( remainder );
            if( req.ErrorMessage != null )
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                ctx.Response.Headers.Add( "ErrorMsg", req.ErrorMessage );
                return Task.CompletedTask;
            }
            var db = _dbProvider.ComponentDB;
            IReadOnlyList<Component> components;
            if( req.Version != null || req.IsLastVersion )
            {
                components = db.ResolveLocalDependencies( monitor, req.Name, req.Target, req.Version );
            }
            else
            {
                Func<SVersion,bool> filter = FilterPreview;
                if( req.VersionMoniker == "release" ) filter = FilterRelease;
                components = db.ResolveLocalDependencies( monitor, req.Name, req.Target, filter );
            }
            if( components == null )
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }
            var rootComponent = components[0];
            Debug.Assert( rootComponent.Name == req.Name );
            Debug.Assert( rootComponent.TargetFramework.CanWorkOn( req.Target ) );

            ctx.Response.StatusCode = StatusCodes.Status200OK;
            var contentDisposition = new ContentDispositionHeaderValue( "attachment" );
            contentDisposition.SetHttpFileName( $"{rootComponent.Name}.{rootComponent.TargetFramework}.{rootComponent.Version}.zip" );
            ctx.Response.GetTypedHeaders().ContentDisposition = contentDisposition;

            return _dbProvider.ExportZippedComponentFiles( monitor, components, ctx.Response.Body );
        }

        static bool FilterPreview( SVersion v )
        {
            // This is a shortcut to avoid parsing a CSemVer for release.
            if( v.Prerelease == null ) return true;
            // If it is a CSemVer version, it is a release or a pre-release:
            // CI-builds are not CSemVer.
            return CSVersion.TryParse( v.Text ).IsValidSyntax;
        }

        static bool FilterRelease( SVersion v )
        {
            return v.Prerelease == null;
        }

        #region Pull

        async Task HandlePull( HttpContext ctx, IActivityMonitor monitor )
        {
            using( var buffer = new MemoryStream() )
            {
                await ctx.Request.Body.CopyToAsync( buffer );
                buffer.Position = 0;
                var missing = new ComponentMissingDescription( XElement.Load( buffer ) );

                ComponentDB db = _dbProvider.ComponentDB;
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
            var content = _dbProvider.OpenFileStream( monitor, sha1 );
            if( content == null )
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            }
            else
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                await content.CopyToAsync( ctx.Response.Body );
            }
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
            monitor.Info( $"Pushing with Api key (SHA1={SHA1Value.ComputeSHA1( apiKey ).ToString()})." );
            using( var buffer = new MemoryStream() )
            {
                await ctx.Request.Body.CopyToAsync( buffer );
                buffer.Position = 0;
                var result = _dbProvider.ImportComponents( monitor, buffer );

                ctx.Response.StatusCode = result.ErrorText != null 
                                               ? StatusCodes.Status500InternalServerError 
                                               : StatusCodes.Status200OK;
                buffer.Position = 0;
                result.Write( new CKBinaryWriter( buffer, Encoding.UTF8, true ) );
                await ctx.Response.Body.WriteAsync( buffer.GetBuffer(), 0, (int)buffer.Position );
            }
        }

        async Task HandlePushFile( HttpContext ctx, IActivityMonitor monitor, PathString shaPath )
        {
            var sessionId = (string)ctx.Request.Headers[ClientRemoteStore.SessionIdHeader];
            SHA1Value sha1 = _dbProvider.ValidPushFileRequest( monitor, shaPath, sessionId );
            if( sha1.IsZero )
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            using( monitor.OpenInfo( $"PushFile: SessionId={sessionId}, SHA1={sha1}." ) )
            {
                ctx.Response.StatusCode = await _dbProvider.HandlePushFileAsync( monitor, sha1, ctx.Request.Body );
            }
        }

        #endregion


    }
}
