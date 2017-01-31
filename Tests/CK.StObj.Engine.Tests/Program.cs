using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests
{
    public class Program
    {
        public static int Main( string[] args )
        {
            string nunit = Path.Combine(TestHelper.SolutionFolder, "packages", "NUnit.Runners.Net4.2.6.4", "tools", "nunit.exe");
            string arg = Path.Combine(TestHelper.BinFolder, "CK.StObj.Engine.Tests.exe");
            Process.Start(nunit, '"' + arg + '"');
            return 0;
        } 

    }
}
