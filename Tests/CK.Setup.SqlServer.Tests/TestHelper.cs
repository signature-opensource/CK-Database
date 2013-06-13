using System.IO;
using NUnit.Framework;
using CK.Core;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace CK.Setup.SqlServer.Tests
{
    static class TestHelper
    {
        static IDefaultActivityLogger _logger;
        static ActivityLoggerConsoleSink _console;
        static string _scriptFolder;

        static TestHelper()
        {
            _console = new ActivityLoggerConsoleSink();
            _logger = new DefaultActivityLogger();
            _logger.Tap.Register( _console );
        }

        public static IActivityLogger Logger
        {
            get { return _logger; }
        }

        public static bool LogsToConsole
        {
            get { return _logger.Tap.RegisteredSinks.Contains( _console ); }
            set
            {
                if( value ) _logger.Tap.Register( _console );
                else _logger.Tap.Unregister( _console );
            }
        }

        #region Trace for IDependentItem

        public static void Trace( IEnumerable<IDependentItem> e )
        {
            using( _logger.OpenGroup( LogLevel.Trace, "Dependent items" ) )
            {
                foreach( var i in e ) Trace( i );
            }
        }

        public static void Trace( IDependentItem i )
        {
            using( _logger.OpenGroup( LogLevel.Trace, "FullName = {0}", i.FullName ) )
            {
                _logger.Trace( "Container = {0}", OneName( i.Container ) );
                _logger.Trace( "Generalization = {0}", OneName( i.Generalization ) );
                _logger.Trace( "Requires = {0}", Names( i.Requires ) );
                _logger.Trace( "RequiredBy = {0}", Names( i.RequiredBy ) );
                _logger.Trace( "Groups = {0}", Names( i.Groups ) );
                IDependentItemGroup g = i as IDependentItemGroup;
                if( g != null )
                {
                    IDependentItemContainerTyped c = i as IDependentItemContainerTyped;
                    if( c != null )
                    {
                        _logger.Trace( "[{0}]Children = {1}", c.ItemKind.ToString()[0], Names( g.Children ) );
                    }
                    else _logger.Trace( "[G]Children = {0}", Names( g.Children ) );
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
            using( _logger.OpenGroup( LogLevel.Trace, "Sorted items" ) )
            {
                foreach( var i in e )
                    if( i.HeadForGroup == null || skipGroupTail )
                        Trace( i );
            }
        }

        public static void Trace( ISortedItem i )
        {
            using( _logger.OpenGroup( LogLevel.Trace, "[{1}]FullName = {0}", i.FullName, i.ItemKind.ToString()[0] ) )
            {
                _logger.Trace( "Container = {0}", i.Container != null ? i.Container.FullName : "(null)" );
                _logger.Trace( "Generalization = {0}", i.Generalization != null ? i.Generalization.FullName : "(null)" );
                _logger.Trace( "Requires = {0}", Names( i.Requires ) );
                _logger.Trace( "Groups = {0}", Names( i.Groups ) );
                _logger.Trace( "Children = {0}", Names( i.Children ) );
            }
        }

        static string Names( IEnumerable<ISortedItem> ee )
        {
            return ee != null ? String.Join( ", ", ee.Select( o => o.FullName ) ) : String.Empty;
        }
        #endregion


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
            string p = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            // Code base is like "file:///C:/Users/Spi/Documents/Dev4/CK-Database/Output/Tests/Debug/NET40/CK.Setup.SqlServer.Tests.DLL"
            StringAssert.StartsWith( "file:///", p, "Code base must start with file:/// protocol." );

            p = p.Substring( 8 ).Replace( '/', System.IO.Path.DirectorySeparatorChar );

            // => NET40/
            p = Path.GetDirectoryName( p );
            // => Debug/
            p = Path.GetDirectoryName( p );
            // => Tests/
            p = Path.GetDirectoryName( p );
            // => Output/
            p = Path.GetDirectoryName( p );
            // => CK-Database/
            p = Path.GetDirectoryName( p );
            // ==> Tests/CK.Setup.SqlServer.Tests/Scripts
            _scriptFolder = Path.Combine( p, "Tests", "CK.Setup.SqlServer.Tests", "Scripts" );
        }
    }
}
