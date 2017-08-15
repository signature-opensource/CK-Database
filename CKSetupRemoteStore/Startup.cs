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

namespace CKSetupRemoteStore
{
    public class Startup
    {
        public void ConfigureServices( IServiceCollection services )
        {
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
            app.UseMiddleware<CKSetupStoreMiddleware>( monitor, new CKSetupStoreMiddlewareOptions()
            {
                RootStorePath = Path.Combine( env.ContentRootPath, "Store" )
            } );
            app.Run( async ( context ) =>
             {
                 await context.Response.WriteAsync( "Hello World!" );
             } );
            monitor.MonitorEnd();
        }
    }
}
