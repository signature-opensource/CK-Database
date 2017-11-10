using CK.Core;
using CK.Monitoring;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CKSetupRemoteStore
{
    public class Program
    {
        public static void Main( string[] args )
        {
            var host = CreateBuilder( args ).Build();
            host.Run();
        }

        static IWebHostBuilder CreateBuilder( string[] args )
        {
            var builder = new WebHostBuilder()
                     .UseUrls( "http://localhost:2982" )
                     .UseKestrel()
                     .UseContentRoot( Directory.GetCurrentDirectory() )
                     .ConfigureAppConfiguration( ( hostingContext, config ) =>
                     {
                        config.AddJsonFile( "appsettings.json", optional: false, reloadOnChange: true );
                        config.AddEnvironmentVariables();
                        if( args != null ) config.AddCommandLine( args );
                     } )
                     .UseMonitoring()
                     .UseIISIntegration()
                     .UseDefaultServiceProvider( ( context, options ) =>
                     {
                        options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                     } )
                     .UseStartup<Startup>();

            if( args != null )
            {
                builder.UseConfiguration( new ConfigurationBuilder().AddCommandLine( args ).Build() );
            }
            return builder;
        }
    }
}
