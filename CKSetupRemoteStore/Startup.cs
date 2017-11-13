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
            services.Configure<CKSetupStoreMiddlewareOptions>( Configuration.GetSection("store") );
            services.AddMemoryCache();
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
            app.UseMiddleware<CKSetupStoreMiddleware>( monitor );
            app.Run( async ( context ) =>
             {
                 var a = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute( Assembly.GetExecutingAssembly(), typeof( AssemblyInformationalVersionAttribute ) );
                 var v = new InformationalVersion( a?.InformationalVersion );

                 await context.Response.WriteAsync( "<html><body>" );
                 await context.Response.WriteAsync( $"<h1>Welcome to {env.ApplicationName}.</h1>Version: {v.ToString()}.<br>" );
                 await context.Response.WriteAsync( $"<br>Latest (CI)<br>" );
                 await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/Net461/ci\">CKSetup.zip (Net461)</a><br>" );
                 await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/NetCoreApp20/ci\">CKSetup.zip (NetCoreApp20)</a>" );
                 await context.Response.WriteAsync( $"<br>Current Preview<br>" );
                 await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/Net461/preview\">CKSetup.zip (Net461)</a><br>" );
                 await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/NetCoreApp20/preview\">CKSetup.zip (NetCoreApp20)</a>" );
                 await context.Response.WriteAsync( $"<br>Current Release<br>" );
                 await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/Net461/release\">CKSetup.zip (Net461)</a><br>" );
                 await context.Response.WriteAsync( $"- <a href=\"/dl-zip/CKSetup/NetCoreApp20/release\">CKSetup.zip (NetCoreApp20)</a>" );
                 await context.Response.WriteAsync( "</body></html>" );
             } );
            monitor.MonitorEnd();
        }
    }
}
