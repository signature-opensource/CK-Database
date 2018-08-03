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
using System.Reflection;

namespace CK.StObj.Engine.Tests
{
    static class TestHelper
    {
        static IActivityMonitor _monitor;
        static ActivityMonitorConsoleClient _console;

        static TestHelper()
        {
            _monitor = new ActivityMonitor();
            _monitor.Output.BridgeTarget.HonorMonitorFilter = false;
            _console = new ActivityMonitorConsoleClient();
        }

        public static IActivityMonitor Monitor
        {
            get { return _monitor; }
        }

        public static Assembly Assembly => typeof( TestHelper ).GetTypeInfo().Assembly;

        public static bool LogsToConsole
        {
            get { return _monitor.Output.Clients.Contains( _console ); }
            set
            {
                if( value )
                {
                    _monitor.Output.RegisterUniqueClient( c => c == _console, () => _console );
                    _monitor.Info( "Console log is ON." );
                }
                else
                {
                    _monitor.Info( "Console log is OFF." );
                    _monitor.Output.UnregisterClient( _console );
                }
            }
        }

        /// <summary>
        /// Loads an assembly that must be in probe paths in .Net framework and in
        /// AppContext.BaseDirectory in .Net Core.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly to load (without any .dll suffix).</param>
        /// <returns>The loaded assembly.</returns>
        static public Assembly LoadAssemblyFromAppContextBaseDirectory( string assemblyName )
        {
#if NET461
            return Assembly.Load( new AssemblyName( assemblyName ) );
#else
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath( Path.Combine( AppContext.BaseDirectory, assemblyName + ".dll" ) );
#endif
        }

        #region Trace for IDependentItem

        public static void TraceDependentItem( this IActivityMonitor @this, IEnumerable<IDependentItem> e )
        {
            using( @this.OpenTrace( "Dependent items" ) )
            {
                foreach( var i in e ) TraceDependentItem( @this, i );
            }
        }

        public static void TraceDependentItem( this IActivityMonitor @this, IDependentItem i )
        {
            using( _monitor.OpenTrace( "FullName = " + i.FullName ) )
            {
                _monitor.Trace( "Container = " + OneName( i.Container ) );
                _monitor.Trace( "Generalization = " + OneName( i.Generalization ) );
                _monitor.Trace( "Requires = " + Names( i.Requires ) );
                _monitor.Trace( "RequiredBy = " + Names( i.RequiredBy ) );
                _monitor.Trace( "Groups = " + Names( i.Groups ) );
                IDependentItemGroup g = i as IDependentItemGroup;
                if( g != null )
                {
                    IDependentItemContainerTyped c = i as IDependentItemContainerTyped;
                    if( c != null )
                    {
                        _monitor.Trace( $"[{c.ItemKind.ToString()[0]}]Children = {Names( g.Children )}"  );
                    }
                    else _monitor.Trace( "[G]Children = " + Names( g.Children ) );
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
            using( _monitor.OpenTrace( "Sorted items" ) )
            {
                foreach( var i in e )
                    if( i.HeadForGroup == null || skipGroupTail )
                        TraceSortedItem( @this, i );
            }
        }

        public static void TraceSortedItem( this IActivityMonitor @this, ISortedItem i )
        {
            using( _monitor.OpenTrace( $"[{i.ItemKind.ToString()[0]}]FullName = {i.FullName}"  ) )
            {
                _monitor.Trace( "Container = " + (i.Container != null ? i.Container.FullName : "(null)") );
                _monitor.Trace( "Generalization = " + (i.Generalization != null ? i.Generalization.FullName : "(null)") );
                _monitor.Trace( "Requires = " + Names( i.Requires ) );
                _monitor.Trace( "Groups = " + Names( i.Groups ) );
                _monitor.Trace( "Children = " + Names( i.Children ) );
            }
        }

        static string Names( IEnumerable<ISortedItem> ee )
        {
            return ee != null ? String.Join( ", ", ee.Select( o => o.FullName ) ) : String.Empty;
        }
        #endregion

        public static void CheckChildren<T>( this StObjCollectorResult @this, string childrenTypeNames )
        {
            Check( @this, @this.StObjMap.ToStObj( typeof( T ) ).Children, childrenTypeNames );
        }

        public static void Check( this StObjCollectorResult @this, IEnumerable<IStObjResult> items, string typeNames )
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
