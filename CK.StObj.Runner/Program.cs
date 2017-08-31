using System;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using CK.Setup;
using CK.Core;
using System.Diagnostics;

#if !NET461
using Microsoft.Extensions.DependencyModel;
#endif

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
            var m = new ActivityMonitor();
            m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            using( m.OpenInfo( "Starting CK.StObj.Runner" ) )
            {
                try
                {
                    if( Array.IndexOf( args, "merge-deps" ) >= 0 )
                    {
                        MergeDeps( m );
                        return 0;
                    }
                    return Run( m ) ? 0 : 1;
                }
                catch( Exception ex )
                {
                    m.Fatal( ex );
                    return -1;
                }
            }
        }

        static void MergeDeps( IActivityMonitor m )
        {
#if NET461
            m.Error( "Invalid merge-deps argument in .Net Framework." );
#else
            using( m.OpenInfo( "Merging deps.json files." ) )
            {
                DependencyContext c = DependencyContext.Default;
                using( var depsReader = new DependencyContextJsonReader() )
                {
                    foreach( var file in Directory.EnumerateFiles( AppContext.BaseDirectory, "*.deps.json" ) )
                    {
                        var name = Path.GetFileName( file );
                        if( name == "CK.StObj.Runner.deps.json" ) continue;
                        m.Info( $"Merging '{name}'." );
                        using( var content = File.OpenRead( file ) )
                        {
                            DependencyContext other = depsReader.Read( content );
                            c = c.Merge( other );
                        }
                    }
                }
                m.Info( "Saving 'CK.StObj.Runner.deps.json.merged'." );
                var path = Path.Combine( AppContext.BaseDirectory, "CK.StObj.Runner.deps.json.merged" );
                var writer = new DependencyContextWriter();
                using( var output = File.Open( path, FileMode.Create, FileAccess.Write, FileShare.None ) )
                {
                    writer.Write( c, output );
                }
            }
#endif
        }

        static bool Run( IActivityMonitor m )
        {
            InstallLoadHooks( m );
            XElement root = XDocument.Load( Path.Combine( AppContext.BaseDirectory, XmlFileName ) ).Root;
            m.MinimalFilter = LogFilter.Parse( root.Element( xLogFiler ).Value );
            var config = new StObjEngineConfiguration( root.Element( xSetup ) );
            return StObjContextRoot.Build( config, null, m );
        }

        static void InstallLoadHooks( IActivityMonitor monitor )
        {
#if NET461
            //monitor.Info( $"CurrentDomain.RelativeSearchPath: {AppDomain.CurrentDomain.RelativeSearchPath}" );
            //AppDomain.CurrentDomain.AssemblyResolve += ( object sender, ResolveEventArgs a ) =>
            //{
            //    var failed = new AssemblyName( a.Name );
            //    var resolved = failed.Version != null && failed.CultureName == null
            //            ? Assembly.Load( new AssemblyName( failed.Name ) )
            //            : null;
            //    monitor.Info( $"AssemblyResolve: {a.Name} ==> {resolved?.FullName}" );
            //    return resolved;
            //};
#endif

#if NETCOREAPP2_0
            monitor.Info( $"CurrentDomain.RelativeSearchPath: {AppDomain.CurrentDomain.RelativeSearchPath}" );
            AppDomain.CurrentDomain.AssemblyResolve += ( object sender, ResolveEventArgs a ) =>
            {
                var failed = new AssemblyName( a.Name );
                var resolved = failed.Version != null && failed.CultureName == null
                        ? Assembly.Load( new AssemblyName( failed.Name ) )
                        : null;
                monitor.Info( $"AssemblyResolve: {a.Name} ==> {resolved?.FullName}" );
                return resolved;
            };
#endif
        }

    }
}
