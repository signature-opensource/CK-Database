using System;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using CK.Setup;
using CK.Core;
using System.Diagnostics;

namespace CK.StObj.Runner
{
    public static partial class Program
    {
        static public int Main( string[] args )
        {
            if( Array.IndexOf( args, "launch-debugger" ) >= 0 )
            {
                Debugger.Launch();
            }
#if NET461
            AppDomain.CurrentDomain.AssemblyResolve += ( object sender, ResolveEventArgs a ) =>
            {
                var failed = new AssemblyName( a.Name );
                return failed.Version != null && failed.CultureName == null
                        ? Assembly.Load( new AssemblyName( failed.Name ) )
                        : null;
            };
#endif
#if NETCOREAPP2_0

#endif
            return Run() ? 0 : 1;
        }

        static bool Run()
        {
            var m = new ActivityMonitor();
            m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            using( m.OpenInfo( "Starting CK.StObj.Runner" ) )
            {
                try
                {
                    XElement root = XDocument.Load( Path.Combine( AppContext.BaseDirectory, XmlFileName ) ).Root;
                    m.MinimalFilter = LogFilter.Parse( root.Element( xLogFiler ).Value );
                    var config = new StObjEngineConfiguration( root.Element( xSetup ) );
                    return StObjContextRoot.Build( config, null, m );
                }
                catch( Exception ex )
                {
                    m.Fatal( ex );
                    return false;
                }
            }
        }

    }
}
