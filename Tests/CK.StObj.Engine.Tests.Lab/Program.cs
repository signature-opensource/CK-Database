using CK.Core;
using System;

namespace TempTestSimpleEmitSrc
{
    class Program
    {
        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;

        static Program()
        {
            _monitor = new ActivityMonitor();
            _monitor.Output.BridgeTarget.HonorMonitorFilter = false;
            _console = new ActivityMonitorConsoleClient();
            _monitor.Output.RegisterClient( _console );
        }

        const string ctorParam = "Protected Ctor is called by public's finalType's constructor.";

        class StObjRuntimeBuilder : IStObjRuntimeBuilder
        {
            public object CreateInstance( Type finalType )
            {
                if( typeof( CK.StObj.Engine.Tests.DynamicGenerationTests.CSimpleEmit.B ).IsAssignableFrom( finalType ) ) return Activator.CreateInstance( finalType, ctorParam );
                else return Activator.CreateInstance( finalType, false );
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            try
            {
                var g = new CK.StObj.GeneratedRootContext( _monitor, new StObjRuntimeBuilder() );
            }
            catch( Exception ex )
            {
                _monitor.Error().Send( ex );
            }
            Console.ReadLine();
        }
    }
}