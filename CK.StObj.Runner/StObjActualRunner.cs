using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Reflection;

#if !NET461
using Microsoft.Extensions.DependencyModel;
#endif

namespace CK.StObj.Runner
{
    static class StObjActualRunner
    {

        internal static int Run( StringBuilder rawLogText, string[] args )
        {
            using( var pipeClient = GetPipeLogClient( args ) )
            {
                var m = new ActivityMonitor();
                if( pipeClient == null ) m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
                else
                {
                    m.Output.RegisterClient( pipeClient );
                }
                using( m.OpenInfo( "Starting CK.StObj.Runner" ) )
                {
                    try
                    {
                        if( Array.IndexOf( args, "merge-deps" ) >= 0 )
                        {
                            MergeDeps( m );
                            return 0;
                        }
                        XElement root;
                        try
                        {
                            root = XDocument.Load( Path.Combine( AppContext.BaseDirectory, Program.XmlFileName ) ).Root;
                            ActivityMonitor.DefaultFilter = LogFilter.Parse( root.Element( Program.xLogFiler ).Value );
                        }
                        catch( Exception ex )
                        {
                            m.Fatal( ex );
                            return -2;
                        }
                        var config = new StObjEngineConfiguration( root.Element( Program.xSetup ) );
                        Type runnerType = SimpleTypeFinder.WeakResolver( "CK.Setup.StObjEngine, CK.StObj.Engine", true );
                        object runner = Activator.CreateInstance( runnerType, m, config, null );
                        MethodInfo mRun = runnerType.GetMethod( "Run" );
                        return (bool)mRun.Invoke( runner, Array.Empty<object>() ) ? 0 : 1;
                    }
                    catch( Exception ex )
                    {
                        m.Fatal( ex );
                        return -1;
                    }
                    finally
                    {
                        if( rawLogText.Length > 0 )
                        {
                            using( m.OpenTrace( "Raw logs:" ) )
                            {
                                m.Trace( rawLogText.ToString() );
                            }
                        }
                    }
                }
            }
        }

        static ActivityMonitorAnonymousPipeLogSenderClient GetPipeLogClient( string[] args )
        {
            foreach( var a in args )
            {
                if( a.StartsWith( "/logPipe:" ) && a.Length > 9 )
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

    }
}
