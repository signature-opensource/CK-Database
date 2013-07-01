using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{
    static class TestHelper
    {
        static IDefaultActivityLogger _logger;
        static ActivityLoggerConsoleSink _console;
        static string _scriptFolder;
        static string _binFolder;
        static string _tempFolder;

        static TestHelper()
        {
            _console = new ActivityLoggerConsoleSink();
            _logger = new DefaultActivityLogger( true );
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

        public static void TraceDependentItem( this IActivityLogger @this, IEnumerable<IDependentItem> e )
        {
            using( @this.OpenGroup( LogLevel.Trace, "Dependent items" ) )
            {
                foreach( var i in e ) TraceDependentItem( @this, i );
            }
        }

        public static void TraceDependentItem( this IActivityLogger @this, IDependentItem i )
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

        public static void TraceSortedItem( this IActivityLogger @this, IEnumerable<ISortedItem> e, bool skipGroupTail )
        {
            using( _logger.OpenGroup( LogLevel.Trace, "Sorted items" ) )
            {
                foreach( var i in e )
                    if( i.HeadForGroup == null || skipGroupTail )
                        TraceSortedItem( @this, i );
            }
        }

        public static void TraceSortedItem( this IActivityLogger @this, ISortedItem i )
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

        public static string BinFolder
        {
            get { if( _binFolder == null ) InitalizePaths(); return _binFolder; }
        }

        public static string TempFolder
        {
            get { if( _tempFolder == null ) InitalizePaths(); return _tempFolder; }
        }

        public static string GetScriptsFolder( string testName )
        {
            return Path.Combine( FolderScript, testName );
        }

        private static void InitalizePaths()
        {
            string p = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            // Code base is like "file:///C:/Users/Spi/Documents/Dev4/CK-Database/Output/Tests/Debug/CK.StObj.Engine.Tests.DLL"
            StringAssert.StartsWith( "file:///", p, "Code base must start with file:/// protocol." );

            p = p.Substring( 8 ).Replace( '/', System.IO.Path.DirectorySeparatorChar );

            // => Debug/
            _binFolder = p = Path.GetDirectoryName( p );
            
            // => Tests/
            p = Path.GetDirectoryName( p );
            _tempFolder = Path.Combine( p, "Temp" ); // => Output/Tests/Temp
            
            // => Output/
            p = Path.GetDirectoryName( p );
            
            // => CK-Database/
            p = Path.GetDirectoryName( p );
            // ==> Tests/CK.StObj.Engine.Tests/Scripts
            _scriptFolder = Path.Combine( p, "Tests", "CK.StObj.Engine.Tests", "Scripts" );
        }

        public static void CheckChildren<T>( this StObjCollectorContextualResult @this, string childrenTypeNames )
        {
            Check( @this, @this.StObjMap.ToStObj( typeof(T) ).Children, childrenTypeNames );
        }

        public static void Check( this StObjCollectorContextualResult @this, IEnumerable<IStObjRuntime> items, string typeNames )
        {
            var s1 = items.Select( i => i.ObjectType.Name ).OrderBy( Util.FuncIdentity );
            var s2 = typeNames.Split( ',' ).OrderBy( Util.FuncIdentity );
            if( !s1.SequenceEqual( s2 ) )
            {
                Assert.Fail( "Expecting '{0}' but was '{1}'.", String.Join( ", ", s2 ), String.Join( ", ", s1 ) );
            }
        }

    }
}
