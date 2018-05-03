using CK.Core;
using FluentAssertions;

namespace CK.StObj.Engine.Tests.SimpleObjects.LoggerInjection
{
    public class LoggerInjected : IAmbientContract
    {
        void StObjConstruct( IActivityMonitor monitor, IActivityMonitor anotherLogger = null )
        {
            monitor.Should().NotBeNull( "This is the Setup monitor. Parameter must be exactly 'IActivityMonitor monitor'." );
            anotherLogger.Should().BeNull( "This is NOT the Setup monitor. Since it is optional, it works." );
            monitor.Trace( "Setup monitor can be used by StObjConstruct method.");
        }
    }
}
