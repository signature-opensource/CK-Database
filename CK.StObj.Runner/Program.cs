using System;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using CK.Setup;
using CK.Core;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

#if !NET461
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
#endif

namespace CK.StObj.Runner
{
    public static partial class Program
    {
        static public int Main( string[] args )
        {
            // See https://github.com/dotnet/corefx/issues/23608
            CultureInfo.CurrentCulture
                = CultureInfo.CurrentUICulture
                = CultureInfo.DefaultThreadCurrentCulture
                = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo( "en-US" );

            if( Array.IndexOf( args, "launch-debugger" ) >= 0 )
            {
                Debugger.Launch();
            }

            using( var pipeClient = GetPipeLogClient( args ) )
            {
                var m = new ActivityMonitor();
                if( pipeClient == null ) m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
                else
                {
                    m.Output.RegisterClient( pipeClient );
                }
                XElement root;
                try
                {
                    root = XDocument.Load( Path.Combine( AppContext.BaseDirectory, XmlFileName ) ).Root;
                    ActivityMonitor.DefaultFilter = LogFilter.Parse( root.Element( xLogFiler ).Value );
                }
                catch( Exception ex )
                {
                    m.Fatal( ex );
                    return -2;
                }
                InstallLoadHooks( m );
                using( m.OpenInfo( "Starting CK.StObj.Runner" ) )
                {
                    try
                    {
                        if( Array.IndexOf( args, "merge-deps" ) >= 0 )
                        {
                            MergeDeps( m );
                            return 0;
                        }
                        var config = new StObjEngineConfiguration( root.Element( xSetup ) );
                        return StObjContextRoot.Build( config, null, m ) ? 0 : 1;
                    }
                    catch( Exception ex )
                    {
                        m.Fatal( ex );
                        return -1;
                    }
                }
            }
        }

        static ActivityMonitorAnonymousPipeLogSenderClient GetPipeLogClient( string[] args )
        {
            foreach( var a in args )
            {
                if( a.StartsWith("/logPipe:" ) && a.Length > 9 )
                {
                    return new ActivityMonitorAnonymousPipeLogSenderClient( a.Substring( 9 ) );
                }
            }
            return null;
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


        static void InstallLoadHooks( IActivityMonitor monitor )
        {
#if NETCOREAPP2_0
            monitor?.Info( $"CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}" );

            AssemblyLoadContext.Default.Resolving += ( context, assemblyName ) =>
            {
                monitor?.Info( $"AssemblyLoadContext.Default.Resolving: {assemblyName.Name}" );
                string file = Path.Combine( AppContext.BaseDirectory, assemblyName.Name + ".dll" );
                Assembly resolved = null;
                if( File.Exists( file ) )
                {
                    monitor?.Info( $"File '{assemblyName.Name}.dll' exists in BaseDirectory." );
                    try
                    {
                        using( var stream = File.OpenRead( file ) )
                            resolved = context.LoadFromStream( stream );
                    }
                    catch( Exception ex )
                    {
                        monitor?.Error( $"While context.LoadFromStream.", ex );
                    }
                }
                else monitor?.Warn( $"File '{assemblyName.Name}.dll' does not exist in BaseDirectory." );
                monitor?.Info( $"Resolved ==> {resolved?.FullName}" );
                return resolved;
            };

            //AppDomain.CurrentDomain.AssemblyResolve += ( object sender, ResolveEventArgs a ) =>
            //{
            //    var failed = new AssemblyName( a.Name );
            //    var resolved = failed.Version != null && failed.CultureName == null
            //            ? Assembly.Load( new AssemblyName( failed.Name ) )
            //            : null;
            //    monitor.Info( $"AssemblyResolve (weaken): {a.Name} ==> {resolved?.FullName}" );
            //    if( resolved == null )
            //    {
            //        string file = Path.Combine( AppContext.BaseDirectory, failed.Name + ".dll" );
            //        if( File.Exists( file ) )
            //        {
            //            monitor.Info( $"File '{failed.Name}.dll' exists in BaseDirectory." );
            //            resolved = Assembly.LoadFrom( file );
            //            monitor.Info( $"AssemblyResolve (LoadFrom): {a.Name} ==> {resolved?.FullName}" );
            //        }
            //        else monitor.Warn( $"File '{failed.Name}.dll' does not exist in BaseDirectory." );
            //    }
            //    return resolved;
            //};
#endif
        }

    }
}
