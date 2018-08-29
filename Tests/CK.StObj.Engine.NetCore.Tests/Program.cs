using CK.Core;
using NUnitLite;
using System.Globalization;
using System.Reflection;

namespace CK.StObj.Engine.Tests.NetCore
{
    public static class Program
    {
        public static int Main( string[] args )
        {
            // ActivityMonitor.AutoConfiguration = m => m.Output.RegisterClient( new ActivityMonitorConsoleClient() );

            CultureInfo.CurrentCulture
                = CultureInfo.CurrentUICulture
                = CultureInfo.DefaultThreadCurrentCulture
                = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo( "en-US" );
            return new AutoRun( Assembly.GetEntryAssembly() ).Execute( args );
        }
    }
}
