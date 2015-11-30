using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.SqlServer.Setup.Engine.Tests
{
    static class TestHelper
    {
        static string _testBinFolder;
        static string _solutionFolder;
        static string _scriptFolder;

        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;

        static TestHelper()
        {
            _monitor = new ActivityMonitor();
            _monitor.Output.BridgeTarget.HonorMonitorFilter = false;
            _console = new ActivityMonitorConsoleClient();
        }

        public const string MasterConnection = "Server=.;Database=master;Integrated Security=SSPI";

        public static IActivityMonitor ConsoleMonitor
        {
            get { return _monitor; }
        }

        public static bool LogsToConsole
        {
            get { return _monitor.Output.Clients.Contains( _console ); }
            set
            {
                if( value ) _monitor.Output.RegisterUniqueClient( c => c == _console, () => _console );
                else _monitor.Output.UnregisterClient( _console );
            }
        }

        #region Trace for IDependentItem

        public static void Trace( IEnumerable<IDependentItem> e )
        {
            using( _monitor.OpenTrace().Send( "Dependent items" ) )
            {
                foreach( var i in e ) Trace( i );
            }
        }

        public static void Trace( IDependentItem i )
        {
            using( _monitor.OpenTrace().Send( "FullName = {0}", i.FullName ) )
            {
                _monitor.Trace().Send( "Container = {0}", OneName( i.Container ) );
                _monitor.Trace().Send( "Generalization = {0}", OneName( i.Generalization ) );
                _monitor.Trace().Send( "Requires = {0}", Names( i.Requires ) );
                _monitor.Trace().Send( "RequiredBy = {0}", Names( i.RequiredBy ) );
                _monitor.Trace().Send( "Groups = {0}", Names( i.Groups ) );
                IDependentItemGroup g = i as IDependentItemGroup;
                if( g != null )
                {
                    IDependentItemContainerTyped c = i as IDependentItemContainerTyped;
                    if( c != null )
                    {
                        _monitor.Trace().Send( "[{0}]Children = {1}", c.ItemKind.ToString()[0], Names( g.Children ) );
                    }
                    else _monitor.Trace().Send( "[G]Children = {0}", Names( g.Children ) );
                }
            }
        }

        static string Names( IEnumerable<IDependentItemRef> ee )
        {
            return ee != null ? String.Join( ", ", ee.Select( o => OneName( o ) ) ) : String.Empty;
        }

        static string OneName( IDependentItemRef o )
        {
            return o != null ? o.FullName + " (" + o.GetType().Name + ")" : "(null)";
        }

        #endregion

        #region Trace for ISortedItem

        public static void Trace( IEnumerable<ISortedItem> e, bool skipGroupTail )
        {
            using( _monitor.OpenTrace().Send( "Sorted items" ) )
            {
                foreach( var i in e )
                    if( i.HeadForGroup == null || skipGroupTail )
                        Trace( i );
            }
        }

        public static void Trace( ISortedItem i )
        {
            using( _monitor.OpenTrace().Send( "[{1}]FullName = {0}", i.FullName, i.ItemKind.ToString() ) )
            {
                _monitor.Trace().Send( "Container = {0}", i.Container != null ? i.Container.FullName : "(null)" );
                _monitor.Trace().Send( "Generalization = {0}", i.Generalization != null ? i.Generalization.FullName : "(null)" );
                _monitor.Trace().Send( "Requires = {0}", Names( i.Requires ) );
                _monitor.Trace().Send( "Groups = {0}", Names( i.Groups ) );
                _monitor.Trace().Send( "Children = {0}", Names( i.Children ) );
            }
        }

        static string Names( IEnumerable<ISortedItem> ee )
        {
            return ee != null ? String.Join( ", ", ee.Select( o => o.FullName ) ) : String.Empty;
        }
        #endregion

        public static string TestBinFolder
        {
            get { if( _testBinFolder == null ) InitalizePaths(); return _testBinFolder; }
        }

        public static string SolutionDirectory
        {
            get { if( _solutionFolder == null ) InitalizePaths(); return _solutionFolder; }
        }

        public static string FolderScript
        {
            get { if( _scriptFolder == null ) InitalizePaths(); return _scriptFolder; }
        }

        public static string GetScriptsFolder( string testName )
        {
            return Path.Combine( FolderScript, testName );
        }

        private static void InitalizePaths()
        {
            string p = new Uri( System.Reflection.Assembly.GetExecutingAssembly().CodeBase ).LocalPath;
            // => CK.XXX.Tests/bin/Debug/
            p = Path.GetDirectoryName( p );
            _testBinFolder = p;
            do
            {
                p = Path.GetDirectoryName( p );
            }
            while( !File.Exists( Path.Combine( p, "CK-Database.sln" ) ) );
            _solutionFolder = p;
            // ==> Tests/CK.SqlServer.Setup.Engine.Tests/Scripts
            _scriptFolder = Path.Combine( p, "Tests", "CK.SqlServer.Setup.Engine.Tests", "Scripts" );
        }
    }
}
