using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace CK.Setup.StObj.Tests.SimpleObjects
{
    public static class SimpleObjectsTrace
    {
        public static StringWriter Log = new StringWriter();

        public static void LogMethod( MethodBase m )
        {
            Log.WriteLine( "{0}.{1}", m.DeclaringType.FullName, m.Name );
        }
    }
}
