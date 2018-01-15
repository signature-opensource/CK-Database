using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using CK.SqlServer.Setup;
using System.Diagnostics;
using static CK.Testing.DBSetupTestHelper;
using System.IO;

namespace CK.DB.Tests
{
    [TestFixture]
    public class DBSetup
    {
        [Test]
        [Explicit]
        public void toggle_logging_to_console()
        {
            TestHelper.LogToConsole = !TestHelper.LogToConsole;
        }

        [Test]
        [Explicit]
        public void existing_generated_dll_load()
        {
            TestHelper.LogToConsole = true;
            Assert.That( TestHelper.LoadStObjMap( TestHelper.GeneratedAssemblyName ) != null, "Generated Assembly must exist and a StObjMap must be loaded." );
        }

        [Test]
        [Explicit]
        public void existing_generated_dll_delete()
        {
            TestHelper.LogToConsole = true;
            var p = TestHelper.BinFolder.AppendPart( TestHelper.GeneratedAssemblyName + ".dll" );
            if( File.Exists( p ) )
            {
                File.Delete( p );
                TestHelper.Monitor.Info( $"Generated assembly '{TestHelper.GeneratedAssemblyName}.dll' removed." );
            }
            else TestHelper.Monitor.Info( $"Generated assembly '{TestHelper.GeneratedAssemblyName}.dll' not found." );
        }

        [Test]
        [Explicit]
        public void attach_debugger()
        {
            TestHelper.LogToConsole = true;
            if( !Debugger.IsAttached) Debugger.Launch();
            else TestHelper.Monitor.Info( "Debugger is already attached." );
        }

        [Test]
        [Explicit]
        public void drop_database()
        {
            TestHelper.LogToConsole = true;
            TestHelper.DropDatabase();
        }

        [Test]
        [Explicit]
        public void db_setup()
        {
            TestHelper.LogToConsole = true;
            Assert.That( TestHelper.RunDBSetup(), "DBSetup failed.");
        }

        [Test]
        [Explicit]
        public void db_setup_with_StObj_and_Setup_graph_ordering_trace()
        {
            TestHelper.LogToConsole = true;
            Assert.That( TestHelper.RunDBSetup( null, true, true ), "DBSetup failed." );
        }

        [Test]
        [Explicit]
        public void db_setup_reverse_with_StObj_and_Setup_graph_ordering_trace()
        {
            TestHelper.LogToConsole = true;
            Assert.That( TestHelper.RunDBSetup( null, true, true, true ), "DBSetup failed." );
        }

        [Test]
        [Explicit]
        public void toggle_CKSetup_LaunchDebug()
        {
            TestHelper.LogToConsole = true;
            TestHelper.CKSetup.DefaultLaunchDebug = !TestHelper.CKSetup.DefaultLaunchDebug;
            TestHelper.Monitor.Info( $"CKSetup/DefaultLaunchDebug is {TestHelper.CKSetup.DefaultLaunchDebug}." );
        }


    }
}
