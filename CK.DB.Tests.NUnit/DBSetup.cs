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
using CKSetup;
using System.Threading;
using FluentAssertions;

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
        public void toggle_CKSetup_LaunchDebug()
        {
            TestHelper.LogToConsole = true;
            TestHelper.CKSetup.DefaultLaunchDebug = !TestHelper.CKSetup.DefaultLaunchDebug;
            TestHelper.Monitor.Info( $"CKSetup/DefaultLaunchDebug is {TestHelper.CKSetup.DefaultLaunchDebug}." );
        }

        [Test]
        [Explicit]
        public void StObjMap_reset()
        {
            TestHelper.LogToConsole = true;
            TestHelper.ResetStObjMap();
            foreach( var p in TestHelper.CKSetup.DefaultBinPaths )
            {
                TestHelper.DeleteGeneratedAssemblies( p );
            }
        }

        [Test]
        [Explicit]
        public void StObjMap_load()
        {
            TestHelper.LogToConsole = true;
            TestHelper.StObjMap.Should().NotBeNull( "StObjMap loading failed." );
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
            var r = TestHelper.RunDBSetup();
            Assert.That( r == CKSetupRunResult.Succeed || r == CKSetupRunResult.UpToDate, "DBSetup failed.");
        }

        [Test]
        [Explicit]
        public void db_setup_with_StObj_and_Setup_graph_ordering_trace()
        {
            TestHelper.LogToConsole = true;
            var r = TestHelper.RunDBSetup( null, true, true );
            Assert.That( r == CKSetupRunResult.Succeed || r == CKSetupRunResult.UpToDate, "DBSetup failed." );
        }

        [Test]
        [Explicit]
        public void db_setup_reverse_with_StObj_and_Setup_graph_ordering_trace()
        {
            TestHelper.LogToConsole = true;
            var r = TestHelper.RunDBSetup( null, true, true, true );
            Assert.That( r == CKSetupRunResult.Succeed || r == CKSetupRunResult.UpToDate, "DBSetup failed." );
        }

        [Test]
        [Explicit]
        public void assembly_load_conflicts_display()
        {
            TestHelper.LogToConsole = true;
            AssemblyLoadConflict[] currents = WeakAssemblyNameResolver.GetAssemblyConflicts();
            Console.WriteLine( $"{currents.Length} assembly load conflicts occurred:" );
            foreach( var c in currents )
            {
                Console.Write( ">>> " );
                Console.WriteLine( c.ToString() );
            }
        }

    }
}
