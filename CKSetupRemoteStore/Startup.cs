using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CK.AspNet;
using CK.Core;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using CSemVer;
using Microsoft.Extensions.FileProviders;

namespace CKSetupRemoteStore
{
    public class Startup
    {
        public Startup( IConfiguration conf )
        {
            Configuration = conf;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices( IServiceCollection services )
        {
            services.AddOptions();
            services.Configure<CKSetupStoreOptions>( Configuration.GetSection("store") );
            services.AddMemoryCache();
            services.AddSingleton<ComponentDBProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure( IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory )
        {
            var monitor = new ActivityMonitor( "Pipeline configuration." );
            if( env.IsDevelopment() )
            {
                loggerFactory.AddConsole();
                app.UseDeveloperExceptionPage();
            }
            app.UseRequestMonitor();
            app.UseMiddleware<CKSetupStoreMiddleware>();
            app.Run( async ( context ) =>
            {
                var dbInfo = context.RequestServices.GetRequiredService<ComponentDBProvider>().Info;
                var a = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute( Assembly.GetExecutingAssembly(), typeof( AssemblyInformationalVersionAttribute ) );
                var v = new InformationalVersion( a?.InformationalVersion );

                await context.Response.WriteAsync( "<html><body>" );
                await context.Response.WriteAsync( $"<h1>Welcome to {env.ApplicationName}.</h1>Version: {v.ToString()}.<br>" );
                await context.Response.WriteAsync( $"<hr><h3>Latest (CI)</h3>" );
                await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/Net461/ci\">CKSetup.zip (Net461)</a><br>" );
                await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/NetCoreApp20/ci\">CKSetup.zip (NetCoreApp20)</a>" );
                await context.Response.WriteAsync( $"<h3>Current Preview</h3>" );
                await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/Net461/preview\">CKSetup.zip (Net461)</a><br>" );
                await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/NetCoreApp20/preview\">CKSetup.zip (NetCoreApp20)</a>" );
                await context.Response.WriteAsync( $"<h3>Current Release</h3>" );
                await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/Net461/release\">CKSetup.zip (Net461)</a><br>" );
                await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/NetCoreApp20/release\">CKSetup.zip (NetCoreApp20)</a>" );
                await context.Response.WriteAsync( $"<hr><h2>Statistics</h2>" );
                await context.Response.WriteAsync( $"- {dbInfo.NamedComponentCount} named components.<br>" );
                await context.Response.WriteAsync( $"- {dbInfo.TotalComponentCount} versioned components covering {dbInfo.TotalComponentCountPerFramework.Count} frameworks:<br>" );
                if( dbInfo.TotalComponentCountPerFramework.Count > 0 )
                {
                    foreach( var fc in dbInfo.TotalComponentCountPerFramework )
                    {
                        await context.Response.WriteAsync( $"&nbsp;&nbsp;- {fc.Key} ({fc.Value} versioned components).<br>" );
                    }
                }
                await context.Response.WriteAsync( $"- Stored Files: {dbInfo.StoredFilesCount} - {dbInfo.StoredTotalFilesSize / 1024} KiB <br>" );
                await context.Response.WriteAsync( $"- Without file sharing: {dbInfo.ComponentsFilesCount} - {dbInfo.ComponentsTotalFilesSize/ 1024} KiB <br>" );
                await WriteFiles( context.Response, dbInfo.BiggestFiles, "Biggest" );
                await context.Response.WriteAsync( "</body></html>" );
            } );
            monitor.MonitorEnd();
        }

        static async Task WriteFiles( HttpResponse context, IReadOnlyList<CKSetup.ComponentFile> files, string title )
        {
            if( files.Count > 0 )
            {
                await context.WriteAsync( $"- {title} files:<br>" );
                foreach( var f in files.Take( 4 ) )
                {
                    await context.WriteAsync( $"&nbsp;&nbsp;- {f.ToDisplayString()}<br>" );
                }
            }
        }
    }
}
