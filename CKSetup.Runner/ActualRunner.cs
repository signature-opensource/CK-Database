using CK.Core;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

#if !NET461
using Microsoft.Extensions.DependencyModel;
#endif

namespace CKSetup.Runner
{
    static class ActualRunner
    {
        static public int Run( StringBuilder rawLogText, string[] args )
        {
            ActivityMonitorAnonymousPipeLogSenderClient pipeClient;
            IActivityMonitor monitor = CreateMonitor( rawLogText, args, out pipeClient );
            using( monitor.OpenLog( LogLevel.Info, "Starting CKSetup.Runner" ) )
            {
                try
                {
                    if( Array.IndexOf( args, "merge-deps" ) >= 0 )
                    {
                        MergeDeps( monitor );
                        return 0;
                    }
                    XElement root = XDocument.Load( Path.Combine( AppContext.BaseDirectory, "CKSetup.Runner.Config.xml" ) ).Root;
                    XElement ckSetup = root.Element( "CKSetup" );
                    string engineAssemblyQualifiedName = ckSetup?.Element( "EngineAssemblyQualifiedName" )?.Value;
                    if( string.IsNullOrWhiteSpace(engineAssemblyQualifiedName) )
                    {
                        monitor.Log( LogLevel.Fatal, "Missing element CKSetup/EngineAssemblyQualifiedName or empty value." );
                        return 3;
                    }
                    Type runnerType = SimpleTypeFinder.WeakResolver( engineAssemblyQualifiedName, true );
                    object runner = Activator.CreateInstance( runnerType, monitor, root );
                    MethodInfo m = runnerType.GetMethod( "Run" );
                    return (bool)m.Invoke( runner, Array.Empty<object>() ) ? 0 : 3;
                }
                catch( Exception ex )
                {
                    monitor.Log( LogLevel.Fatal, ex.Message, ex );
                    return 2;
                }
                finally
                {
                    if( rawLogText.Length > 0 )
                    {
                        using( monitor.OpenLog( LogLevel.Trace, "Raw logs:" ) )
                        {
                            monitor.Log( LogLevel.Trace, rawLogText.ToString() );
                        }
                    }
                    pipeClient?.Dispose();
                }
            }
        }

        static IActivityMonitor CreateMonitor( StringBuilder rawLogText, string[] args, out ActivityMonitorAnonymousPipeLogSenderClient pipeClient )
        {
            pipeClient = null;
            var monitor = new ActivityMonitor();
            foreach( var a in args )
            {
                if( a.StartsWith( "/logPipe:" ) && a.Length > 9 )
                {
                    pipeClient = new ActivityMonitorAnonymousPipeLogSenderClient( a.Substring( 9 ) );
                    monitor.Output.RegisterClient( pipeClient );
                }
                else
                {
                    monitor.Output.RegisterClient( new ActivityMonitorConsoleClient() );
                    monitor.Log( LogLevel.Warn, "Missing /logPipe: parameter. Using Console." );
                    rawLogText.AppendLine( "Missing /logPipe: parameter. Using Console." );
                }
            }
            return monitor;
        }

        static void MergeDeps( IActivityMonitor m )
        {
#if NET461
            throw new ArgumentException( "Invalid merge-deps argument in .Net Framework." );
#else
            using( m.OpenLog( LogLevel.Info, "Merging deps.json files." ) )
            {
                DependencyContext c = DependencyContext.Default;
                using( var depsReader = new DependencyContextJsonReader() )
                {
                    foreach( var file in Directory.EnumerateFiles( AppContext.BaseDirectory, "*.deps.json" ) )
                    {
                        var name = Path.GetFileName( file );
                        if( name == "CKSetup.Runner.deps.json" ) continue;
                        m.Log( LogLevel.Info, $"Merging '{name}'." );
                        using( var content = File.OpenRead( file ) )
                        {
                            DependencyContext other = depsReader.Read( content );
                            c = c.Merge( other );
                        }
                    }
                }
                m.Log( LogLevel.Info, "Saving 'CKSetup.Runner.deps.json.merged'." );
                var path = Path.Combine( AppContext.BaseDirectory, "CKSetup.Runner.deps.json.merged" );
                var writer = new DependencyContextWriter();
                using( var output = File.Open( path, FileMode.Create, FileAccess.Write, FileShare.None ) )
                {
                    writer.Write( c, output );
                }
            }
#endif
        }
    }
}
