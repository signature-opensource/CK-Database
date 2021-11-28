using CK.Core;
using CK.Testing;
using CK.Text;
using CKSetup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;

using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.Tests
{
    /// <summary>
    /// Interactive tests that enable controls of test environment.
    /// </summary>
    [TestFixture]
    public abstract class DBSetup
    {
        static DBSetup()
        {
            var root = TestHelper.SolutionFolder.AppendPart( "Tests" );

            void CheckFile( string testName, string? displayTestName = null )
            {
                var path = root.AppendPart( testName + ".playlist" );
                if( !System.IO.File.Exists( path ) )
                {
                    System.IO.File.WriteAllText( path, $@"<Playlist Version=""2.0"">
  <Rule Name=""Includes"" Match=""Any"">
    <Rule Match=""All"">
      <Property Name=""Solution"" />
      <Rule Match=""All"">
        <Property Name=""Namespace"" Value=""DBSetup"" />
        <Rule Match=""Any"">
          <Rule Match=""All"">
            <Property Name=""Class"" Value=""DBSetup"" />
            <Rule Match=""Any"">
              <Rule Match=""All"">
                <Property Name=""TestWithNormalizedFullyQualifiedName"" Value=""DBSetup.DBSetup.{testName}"" />
                <Rule Match=""Any"">
                  <Property Name=""DisplayName"" Value=""{displayTestName ?? testName}"" />
                </Rule>
              </Rule>
            </Rule>
          </Rule>
        </Rule>
      </Rule>
    </Rule>
  </Rule>
</Playlist>" );
                }
            }

            CheckFile( "db_setup" );
            CheckFile( "drop_database" );
            CheckFile( "db_setup_reverse_with_StObj_and_Setup_graph_ordering_trace" );
            CheckFile( "backup_create" );
            CheckFile( "backup_restore", "backup_restore(&quot;0 - Most recent one.&quot;)" );
        }

        /// <summary>
        /// Toggles <see cref="CK.Testing.Monitoring.IMonitorTestHelperCore.LogToConsole"/>.
        /// </summary>
        [Test]
        [Explicit]
        public void toggle_logging_to_console()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = !TestHelper.LogToConsole;
        }

        /// <summary>
        /// Toggles <see cref="CK.Testing.CKSetup.ICKSetupDriver.DefaultLaunchDebug"/> value.
        /// </summary>
        [Test]
        [Explicit]
        public void toggle_CKSetup_LaunchDebug()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            TestHelper.CKSetup.DefaultLaunchDebug = !TestHelper.CKSetup.DefaultLaunchDebug;
            TestHelper.Monitor.Info( $"CKSetup/DefaultLaunchDebug is {TestHelper.CKSetup.DefaultLaunchDebug}." );
        }

        /// <summary>
        /// Resets the <see cref="CK.Testing.StObjMap.IStObjMapTestHelperCore.StObjMap"/> (and the <see cref="CK.Testing.StObjMap.IStObjMapTestHelperCore.AutomaticServices"/>)
        /// by calling <see cref="CK.Testing.StObjMap.IStObjMapTestHelperCore.ResetStObjMap(bool)"/>
        /// and <see cref="CK.Testing.StObjMap.IStObjMapTestHelperCore.DeleteGeneratedAssemblies(string)"/>
        /// in all the bin folders (<see cref="IBasicTestHelper.BinFolder"/> and all <see cref="CK.Testing.CKSetup.ICKSetupDriver.DefaultBinPaths"/>).
        /// </summary>
        [Test]
        [Explicit]
        public void StObjMap_reset()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            TestHelper.ResetStObjMap();
            TestHelper.DeleteGeneratedAssemblies( TestHelper.BinFolder );
            foreach( var p in TestHelper.CKSetup.DefaultBinPaths )
            {
                TestHelper.DeleteGeneratedAssemblies( p );
            }
        }

        /// <summary>
        /// Attempts to load the <see cref="CK.Testing.StObjMap.IStObjMapTestHelperCore.AutomaticServices"/> that
        /// implies to load the StObjMap and to actually fully configure the Service provider. 
        /// </summary>
        [Test]
        [Explicit]
        public void AutomaticServices_load()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            TestHelper.StObjMap.Should().NotBeNull( "StObjMap loading failed." );
            TestHelper.AutomaticServices.Should().NotBeNull( "AutomaticServices configuration failed." );
        }


        /// <summary>
        /// Attaches the debugger to this test context (simply calls <see cref="Debugger.Launch()"/>).
        /// </summary>
        [Test]
        [Explicit]
        public void attach_debugger()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            if( !Debugger.IsAttached ) Debugger.Launch();
            else TestHelper.Monitor.Info( "Debugger is already attached." );
        }

        /// <summary>
        /// Calls <see cref="CK.Testing.SqlServer.ISqlServerTestHelperCore.DropDatabase"/> on the
        /// default database (<see cref="CK.Testing.SqlServer.ISqlServerTestHelperCore.DefaultDatabaseOptions"/>).
        /// </summary>
        [Test]
        [Explicit]
        public void drop_database()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            TestHelper.DropDatabase();
        }

        /// <summary>
        /// Calls <see cref="CK.Testing.SqlServer.BackupManager.CreateBackup(string?)"/> on the
        /// default database (<see cref="CK.Testing.SqlServer.ISqlServerTestHelperCore.DefaultDatabaseOptions"/>).
        /// </summary>
        [Test]
        [Explicit]
        public void backup_create()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            Assert.That( TestHelper.Backup.CreateBackup() != null, "Backup should be possible." );
        }

        /// <summary>
        /// Calls <see cref="CK.Testing.SqlServer.BackupManager.RestoreBackup(string?, int)"/> on the
        /// default database (<see cref="CK.Testing.SqlServer.ISqlServerTestHelperCore.DefaultDatabaseOptions"/>).
        /// </summary>
        [TestCase( "0 - Most recent one." )]
        [TestCase( "1" )]
        [TestCase( "2" )]
        [TestCase( "3" )]
        [TestCase( "4" )]
        [TestCase( "5" )]
        [TestCase( "X - Oldest one." )]
        [Explicit]
        public void backup_restore( string what )
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            if( !int.TryParse( what, out var index ) )
            {
                index = what[0] == 'X' ? Int32.MaxValue : 0;
            }
            Assert.That( TestHelper.Backup.RestoreBackup( null, index ) != null, "Restoring should be possible." );
        }

        /// <summary>
        /// Dumps all the available backup files in <see cref="CK.Testing.SqlServer.BackupManager.BackupFolder"/>
        /// as information into the <see cref="CK.Testing.Monitoring.IMonitorTestHelperCore.Monitor"/>.
        /// </summary>
        [Test]
        [Explicit]
        public void backup_list()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            var all = TestHelper.Backup.GetAllBackups();
            using( TestHelper.Monitor.OpenInfo( $"There is {all.Count} backups available in '{TestHelper.Backup.BackupFolder}'." ) )
            {
                TestHelper.Monitor.Info( all.Select( a => $"n° {a.Index} - {a.FileName}" ).Concatenate( Environment.NewLine ) );
            }
        }

        /// <summary>
        /// Calls <see cref="CK.Testing.DBSetup.IDBSetupTestHelperCore.RunDBSetup"/>
        /// ans checks that the result is <see cref="CKSetupRunResult.Succeed"/> or <see cref="CKSetupRunResult.UpToDate"/>.
        /// </summary>
        [Test]
        [Explicit]
        public void db_setup()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            var r = TestHelper.RunDBSetup();
            Assert.That( r == CKSetupRunResult.Succeed || r == CKSetupRunResult.UpToDate, "DBSetup failed.");
        }

        /// <summary>
        /// Calls <see cref="CK.Testing.DBSetup.IDBSetupTestHelperCore.RunDBSetup"/> with full
        /// oredering traces.
        /// ans checks that the result is <see cref="CKSetupRunResult.Succeed"/> or <see cref="CKSetupRunResult.UpToDate"/>.
        /// </summary>
        [Test]
        [Explicit]
        public void db_setup_with_StObj_and_Setup_graph_ordering_trace()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            var r = TestHelper.RunDBSetup( null, true, true );
            Assert.That( r == CKSetupRunResult.Succeed || r == CKSetupRunResult.UpToDate, "DBSetup failed." );
        }

        /// <summary>
        /// Calls <see cref="CK.Testing.DBSetup.IDBSetupTestHelperCore.RunDBSetup"/> with full
        /// oredering traces and reverse names.
        /// ans checks that the result is <see cref="CKSetupRunResult.Succeed"/> or <see cref="CKSetupRunResult.UpToDate"/>.
        /// </summary>
        [Test]
        [Explicit]
        public void db_setup_reverse_with_StObj_and_Setup_graph_ordering_trace()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            TestHelper.LogToConsole = true;
            var r = TestHelper.RunDBSetup( null, true, true, true );
            Assert.That( r == CKSetupRunResult.Succeed || r == CKSetupRunResult.UpToDate, "DBSetup failed." );
        }

        /// <summary>
        /// Dumps configuration information, assemblies conflicts and assemblies loaded.
        /// </summary>
        [Test]
        [Explicit]
        public void display_information()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );
            Console.WriteLine( "-------------- TestHelper -------------" );
            DumpProperties( String.Empty, TestHelper );
            DumpProperties( "CKSetup.", TestHelper.CKSetup );

            Console.WriteLine();
            Console.WriteLine( "------------ Configuration ------------" );
            var conf = TestHelperResolver.Default.Resolve<ITestHelperConfiguration>();
            foreach( var cG in conf.ConfigurationValues.GroupBy( e => e.Value.BasePath ).OrderBy( x => x ) )
            {
                Console.WriteLine( $"- Base path: {cG.Key}" );
                foreach( var c in cG.OrderBy( x => x.Key ) )
                {
                    Console.WriteLine( $" - {c.Key} = {c.Value.Value}" );
                }
            }

            Console.WriteLine();
            Console.WriteLine( "---------- Assembly conflicts ----------" );
            AssemblyLoadConflict[] currents = WeakAssemblyNameResolver.GetAssemblyConflicts();
            Console.WriteLine( $"{currents.Length} assembly load conflicts occurred:" );
            foreach( var c in currents )
            {
                Console.Write( ">>> " );
                Console.WriteLine( c.ToString() );
            }
            Console.WriteLine();
            Console.WriteLine( "---------- Loaded Assemblies ----------" );
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Console.WriteLine( $"{assemblies.Length} assemblies loaded:" );
            foreach( var a in assemblies )
            {
                Console.WriteLine( a.ToString() );
            }
        }

        void DumpProperties( string prefix, object o )
        {
            foreach( var p in o.GetType().GetProperties() )
            {
                if( p.PropertyType.IsValueType || p.PropertyType == typeof(string) )
                {
                    Console.WriteLine( $"{prefix}{p.Name} = {p.GetValue( o ) ?? "<null>"}" );
                }
                else if( typeof( System.Collections.IEnumerable ).IsAssignableFrom( p.PropertyType ) )
                {
                    Console.Write( $"{prefix}{p.Name} = " );
                    var items = p.GetValue( o ) as System.Collections.IEnumerable;
                    if( items == null ) Console.WriteLine( "<null>" );
                    else
                    {
                        var wPrefix = new String( ' ', prefix.Length + p.Name.Length + 3 );
                        Console.WriteLine( $"[" );
                        foreach( var item in items )
                        {
                            if( item != null )
                            {
                                Type t = item.GetType();
                                if( t.IsValueType || t == typeof( string ) )
                                {
                                    Console.Write( wPrefix );
                                    Console.WriteLine( item );
                                }
                            }
                        }
                        Console.WriteLine( $"{wPrefix}]" );
                    }
                }
            }
        }

    }
}
