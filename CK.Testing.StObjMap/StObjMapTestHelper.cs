using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using CK.Core;
using CK.Testing.Monitoring;
using CK.Testing.StObjMap;
using CK.Text;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="IStObjMapTestHelperCore"/>.
    /// </summary>
    public class StObjMapTestHelper : IStObjMapTestHelperCore
    {
        readonly ITestHelperConfiguration _config;
        readonly IMonitorTestHelper _monitor;
        readonly string _originGeneratedAssemblyName;
        string _generatedAssemblyName;
        static int _resetNumer;
        IStObjMap _map;
        event EventHandler _stObjMapLoading;

        public StObjMapTestHelper( ITestHelperConfiguration config, IMonitorTestHelper monitor )
        {
            _config = config;
            _monitor = monitor;
            _generatedAssemblyName = _originGeneratedAssemblyName = _config.Get( "StObjMap/GeneratedAssemblyName", StObjEngineConfiguration.DefaultGeneratedAssemblyName );
            if( _generatedAssemblyName.IndexOf( ".Reset.", StringComparison.OrdinalIgnoreCase ) >= 0 )
            {
                throw new ArgumentException( "Must not contain '.Reset.' substring.", "StObjMap/GeneratedAssemblyName" );
            }
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
                    using( _monitor.Monitor.OpenInfo( $"Accessing null StObj map: invoking StObjMapLoading event." ) )
                    {
                        _stObjMapLoading?.Invoke( this, EventArgs.Empty );
                        _map = DoLoadStObjMap( _generatedAssemblyName, true );
                    }
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
#if NET461
                    var a = Assembly.Load( new AssemblyName( assemblyName ) );
#else
                    var a = Assembly.LoadFrom( Path.Combine( AppContext.BaseDirectory, assemblyName + ".dll" ) );
#endif
                    return StObjContextRoot.Load( a, StObjContextRoot.DefaultStObjRuntimeBuilder, _monitor.Monitor );
                }
                catch( Exception ex )
                {
                    _monitor.Monitor.Error( ex );
                    return null;
                }
            }
        }

        public void ResetStObjMap()
        {
            if( _map != null )
            {
                _map = null;
                var num = Interlocked.Increment( ref _resetNumer );
                _generatedAssemblyName = $"{_originGeneratedAssemblyName}.Reset.{num}";
                _monitor.Monitor.Info( $"Reseting StObjMap: Generated assembly name is now: {_generatedAssemblyName}." );
            }
            else _monitor.Monitor.Info( $"StObjMap is not loaded yet." );
        }

        public int DeleteGeneratedAssemblies( string directory )
        {
            using( _monitor.Monitor.OpenInfo( $"Deleting generated assemblies from {directory}." ) )
            {
                var r = new Regex( Regex.Escape( _originGeneratedAssemblyName ) + @"(\.Reset\.\d+)?\.dll", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );
                int count = 0;
                if( Directory.Exists( directory ) )
                {
                    foreach( var f in Directory.EnumerateFiles( directory ) )
                    {
                        if( r.IsMatch( f ) )
                        {
                            _monitor.Monitor.Info( $"Deleting Generated assembly: {f}." );
                            try
                            {
                                File.Delete( f );
                            }
                            catch( Exception ex )
                            {
                                _monitor.Monitor.Error( ex );
                            }
                            ++count;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the <see cref="IStObjMapTestHelper"/> default implementation.
        /// </summary>
        public static IStObjMapTestHelper TestHelper => TestHelperResolver.Default.Resolve<IStObjMapTestHelper>();

    }
}
