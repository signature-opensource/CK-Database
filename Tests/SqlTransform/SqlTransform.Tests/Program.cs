using CK.Monitoring;
using NUnitLite;
using System;
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
            //foreach( var a in AppDomain.CurrentDomain.GetAssemblies() )
            //{
            //    Console.Write( a.FullName );
            //    Console.WriteLine( a.IsDynamic ? " (Dynamic)" : "" );
            //    if( !a.IsDynamic )
            //    {
            //        Console.Write( "  -> CodeBase: " );
            //        Console.WriteLine( a.CodeBase );
            //        Console.Write( "  -> Location: " );
            //        Console.WriteLine( a.Location );
            //    }
            //}
            //Console.ReadKey();
            GrandOutput.Default?.Dispose();
            return r;
        }
    }
}
