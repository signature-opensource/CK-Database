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
            CultureInfo.CurrentCulture
                = CultureInfo.CurrentUICulture
                = CultureInfo.DefaultThreadCurrentCulture
                = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo( "en-US" );
            //Core.TestHelper.LogToConsole = true;
            //Core.TestHelper.Monitor.MinimalFilter = LogFilter.Debug;
            return new AutoRun( Assembly.GetEntryAssembly() ).Execute( args );
        }
    }
}
