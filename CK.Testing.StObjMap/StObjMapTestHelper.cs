using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CK.Core;
using CK.Testing.Monitoring;
using CK.Testing.StObjMap;
using CK.Text;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="IStObjMapTestHelperCore"/>.
    /// </summary>
    public class StObjMapTestHelper : StObjMap.IStObjMapTestHelperCore
    {
        readonly ITestHelperConfiguration _config;
        readonly IMonitorTestHelper _monitor;
        readonly string _generatedAssemblyName;
        IStObjMap _map;
        event EventHandler _stObjMapLoading;

        public StObjMapTestHelper( ITestHelperConfiguration config, IMonitorTestHelper monitor )
        {
            _config = config;
            _monitor = monitor;
            _generatedAssemblyName = _config.Get( "StObjMap/GeneratedAssemblyName", StObjEngineConfiguration.DefaultGeneratedAssemblyName );
        }

        event EventHandler IStObjMapTestHelperCore.StObjMapLoading
        {
            add => _stObjMapLoading += value;
            remove => _stObjMapLoading -= value;
        }

        string StObjMap.IStObjMapTestHelperCore.GeneratedAssemblyName => _generatedAssemblyName;

        IStObjMap StObjMap.IStObjMapTestHelperCore.StObjMap
        {
            get
            {
                if( _map == null )
                {
                    _stObjMapLoading?.Invoke( this, EventArgs.Empty );
                    _map = DoLoadStObjMap( _generatedAssemblyName, true );
                }
                return _map;
            }
        }

        IStObjMap StObjMap.IStObjMapTestHelperCore.LoadStObjMap( string assemblyName, bool withWeakAssemblyResolver )
        {
            return DoLoadStObjMap( assemblyName, withWeakAssemblyResolver );
        }

        IStObjMap DoLoadStObjMap( string assemblyName, bool withWeakAssemblyResolver )
        {
            return withWeakAssemblyResolver
                        ? _monitor.WithWeakAssemblyResolver( () => DoLoadStObjMap( assemblyName ) )
                        : DoLoadStObjMap( assemblyName );
        }

        IStObjMap DoLoadStObjMap( string assemblyName )
        {
            using( _monitor.Monitor.OpenInfo( $"Loading StObj map from {assemblyName}." ) )
            {
                try
                {
                    var a = Assembly.Load( assemblyName );
                    return StObjContextRoot.Load( a, StObjContextRoot.DefaultStObjRuntimeBuilder, _monitor.Monitor );
                }
                catch( Exception ex )
                {
                    _monitor.Monitor.Error( ex );
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IStObjMapTestHelper"/> default implementation.
        /// </summary>
        public static IStObjMapTestHelper TestHelper => TestHelperResolver.Default.Resolve<IStObjMapTestHelper>();

    }
}
