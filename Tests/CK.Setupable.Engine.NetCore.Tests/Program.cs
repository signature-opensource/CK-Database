using NUnitLite;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CK.StObj.Engine.Tests.NetCore
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new AutoRun(Assembly.GetEntryAssembly()).Execute(args);
        }
    }
}
