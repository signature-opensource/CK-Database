using System.IO;
using NUnit.Framework;
using CK.Core;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace CK.Setup.Dependency.Tests
{
    static class TestHelper
    {
        static IDefaultActivityLogger _logger;
        static ActivityLoggerConsoleSink _console;

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
            foreach( var i in e ) Trace( i );
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
            foreach( var i in e )
                if( i.HeadForGroup == null || skipGroupTail ) 
                    Trace( i );
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

    }
}
