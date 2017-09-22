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
        public Startup( IHostingEnvironment env )
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath( env.ContentRootPath )
                    .AddJsonFile( "appsettings.json", optional: true, reloadOnChange: true )
                    .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

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
                 await context.Response.WriteAsync( $"<h1>Welcome to {env.ApplicationName}.</h1>Version: {v.ToString()}." );
                 await context.Response.WriteAsync( "</body></html>" );
             } );
            monitor.MonitorEnd();
        }
    }
}
