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

        public static Assembly Assembly => typeof( TestHelper ).Assembly;

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

        public static SimpleServiceContainer CreateAndConfigureSimpleContainer( IStObjMap map )
        {
            var container = new SimpleServiceContainer();
            container.AddStObjMap( map );
            return container;
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

        public static void CheckChildren<T>( this IStObjObjectEngineMap @this, string childrenTypeNames )
        {
            Check( @this, @this.ToStObj( typeof( T ) ).Children, childrenTypeNames );
        }

        public static void Check( this IStObjObjectEngineMap @this, IEnumerable<IStObjResult> items, string typeNames )
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
