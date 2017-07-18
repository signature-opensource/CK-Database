using System;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using CK.Setup;
using CK.Core;

namespace CK.Setup
{
    public static partial class SetupRunner
    {
        static public int Main( string[] args )
        {
#if NET461
            AppDomain.CurrentDomain.AssemblyResolve += ( object sender, ResolveEventArgs a ) =>
            {
                var failed = new AssemblyName( a.Name );
                return failed.Version != null && failed.CultureName == null
                        ? Assembly.Load( new AssemblyName( failed.Name ) )
                        : null;
            };
#endif
            return Run() ? 0 : 1;
        }

        static bool Run()
        {
            var m = new ActivityMonitor();
            m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            using( m.OpenInfo().Send( "Starting CKSetup.Runner" ) )
            {
                try
                {
                    XElement root = XDocument.Load( Path.Combine( AppContext.BaseDirectory, XmlFileName ) ).Root;
                    m.MinimalFilter = LogFilter.Parse( root.Element( xLogFiler ).Value );
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