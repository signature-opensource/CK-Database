using CK.Monitoring;
using NUnitLite;
using System.Globalization;
using System.Reflection;

namespace CK.StObj.Engine.Tests.NetCore
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            CultureInfo.CurrentCulture
                = CultureInfo.CurrentUICulture
                = CultureInfo.DefaultThreadCurrentCulture
                = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo( "en-US" );
            int r = new AutoRun( Assembly.GetEntryAssembly() ).Execute( args );
            GrandOutput.Default?.Dispose();
            return r;
        }
    }
}
