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
using CK.Testing;

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
            TestHelper.DeleteGeneratedAssemblies( TestHelper.BinFolder );
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
        public void display_information()
        {
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
                            Type t = item.GetType();
                            if( t.IsValueType || t == typeof( string ) )
                            {
                                Console.Write( wPrefix );
                                Console.WriteLine( item );
                            }
                        }
                        Console.WriteLine( $"{wPrefix}]" );
                    }
                }
            }
        }

    }
}
