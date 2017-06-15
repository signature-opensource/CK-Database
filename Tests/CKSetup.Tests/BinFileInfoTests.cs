using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CKSetup.Tests
{
    [TestFixture]
    public class BinFileInfoTests
    {
        [Test]
        public void reading_runtimes_files()
        {
            {
                string setupableRuntime461 = Path.Combine( TestHelper.SolutionFolder, "CK.Setupable.Runtime", "bin", "Debug", "net461" );
                var allFiles = BinFileInfo.ReadBinFolder( TestHelper.ConsoleMonitor, setupableRuntime461 );
                var main = allFiles.Single( f => f.Name.Name == "CK.Setupable.Runtime" );
                var useless = allFiles.Except( main.LocalDependencies );
            }
        }
    }
}
