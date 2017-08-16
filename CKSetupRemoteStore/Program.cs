using CK.Core;
using CK.Monitoring;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace CKSetupRemoteStore
{
    public class Program
    {
        public static void Main( string[] args )
        {
            ActivityMonitor.AutoConfiguration += m => m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            SystemActivityMonitor.RootLogPath = Path.Combine( Directory.GetCurrentDirectory(), "Logs" );
            var c = new GrandOutputConfiguration();
            c.Handlers.Add( new CK.Monitoring.Handlers.TextFileConfiguration() { Path = "Text" } );
            using( GrandOutput.EnsureActiveDefault( c ) )
            {
                var host = new WebHostBuilder()
                    .UseUrls( "http://localhost:2982" )
                    .UseKestrel()
                    .UseContentRoot( Directory.GetCurrentDirectory() )
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .Build();

                host.Run();
            }
        }
    }
}
