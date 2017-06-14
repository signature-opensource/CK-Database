using System;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using CK.Setup;
using CK.Core;

namespace CKSetup.Runner
{
    class Program
    {
        static int Main( string[] args )
        {
            AppDomain.CurrentDomain.AssemblyResolve += ( object sender, ResolveEventArgs a ) =>
            {
                var failed = new AssemblyName( a.Name );
                return failed.Version != null && failed.CultureName == null
                        ? Assembly.Load( new AssemblyName( failed.Name ) )
                        : null;
            };

            return Run() ? 0 : 1;
        }

        static readonly XName xRunner = XNamespace.None + "Runner";
        static readonly XName xLogFiler = XNamespace.None + "LogFilter";
        static readonly XName xSetup = XNamespace.None + "Setup";

        static bool Run()
        {
            var m = new ActivityMonitor();
            m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            using( m.OpenInfo().Send( "Starting CKSetup.Runner" ) )
            {
                try
                {
                    XElement root = XDocument.Load( Path.Combine( AppContext.BaseDirectory, "SetupConf.xml" ) ).Root;
                    XElement runner = root.Element( xRunner );
                    m.MinimalFilter = LogFilter.Parse( runner.Element( xLogFiler ).Value );
                    var config = new SetupEngineConfiguration( root.Element( xSetup ) );
                    return StObjContextRoot.Build( config, null, m );
                }
                catch( Exception ex )
                {
                    m.Fatal().Send( ex );
                    return false;
                }
            }
        }

    }
}