using System.Reflection;
using CK.Core;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    public static class SimpleObjectsTrace
    {
        public static void LogMethod( MethodBase m )
        {
            TestHelper.ConsoleMonitor.Trace().Send( "{0}.{1} {2} has been called.", m.DeclaringType.Name, m.Name, m.IsVirtual ? "(virtual)" : "" );
        }
    }
}
