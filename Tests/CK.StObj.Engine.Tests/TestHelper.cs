#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\TestHelper.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
        static string _scriptFolder;
        static string _binFolder;
        static string _tempFolder;

        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;

        static TestHelper()
        {
            _monitor = new ActivityMonitor();
            _monitor.Output.BridgeTarget.HonorMonitorFilter = false;
            _console = new ActivityMonitorConsoleClient();
            _monitor.Output.RegisterClients( _console );
        }

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

        public static void TraceDependentItem( this IActivityMonitor @this, IEnumerable<IDependentItem> e )
        {
            using( @this.OpenTrace().Send( "Dependent items" ) )
            {
                foreach( var i in e ) TraceDependentItem( @this, i );
            }
        }

        public static void TraceDependentItem( this IActivityMonitor @this, IDependentItem i )
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

        public static void TraceSortedItem( this IActivityMonitor @this, IEnumerable<ISortedItem> e, bool skipGroupTail )
        {
            using( _monitor.OpenTrace().Send( "Sorted items" ) )
            {
                foreach( var i in e )
                    if( i.HeadForGroup == null || skipGroupTail )
                        TraceSortedItem( @this, i );
            }
        }

        public static void TraceSortedItem( this IActivityMonitor @this, ISortedItem i )
        {
            using( _monitor.OpenTrace().Send( "[{1}]FullName = {0}", i.FullName, i.ItemKind.ToString()[0] ) )
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

        public static void Check( this StObjCollectorContextualResult @this, IEnumerable<IStObjResult> items, string typeNames )
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
