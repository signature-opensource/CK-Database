using System.Reflection;
using CK.Core;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public static class SimpleObjectsTrace
    {
        public static void LogMethod( MethodBase m )
        {
            TestHelper.Logger.Trace( "{0}.{1} {2} has been called.", m.DeclaringType.Name, m.Name, m.IsVirtual ? "(virtual)" : "" );
        }
    }
}
