using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Text;
using System.Reflection;

namespace CK.Core
{
    public partial class AmbientTypeCollector
    {
        readonly Dictionary<Type, AmbientServiceClassInfo> _serviceCollector;
        readonly Dictionary<Type, AmbientServiceInterfaceInfo> _serviceInterfaces;

        AmbientServiceClassInfo RegisterServiceClass( Type t, AmbientServiceClassInfo parent )
        {
            var serviceInfo = new AmbientServiceClassInfo( _monitor, _serviceProvider, parent, t, this, !_typeFilter( _monitor, t ) );
            if( !serviceInfo.IsExcluded )
            {
                RegisterAssembly( t );
            }
            _serviceCollector.Add( t, serviceInfo );
            return serviceInfo;
        }

        internal AmbientServiceClassInfo FindServiceClassInfo( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t.IsClass );
            _serviceCollector.TryGetValue( t, out var info );
            return info;
        }
        internal AmbientServiceInterfaceInfo FindServiceInterfaceInfo( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t.IsInterface );
            _serviceInterfaces.TryGetValue( t, out var info );
            return info;
        }

        /// <summary>
        /// Returns null if and only if the type is excluded.
        /// </summary>
        internal AmbientServiceInterfaceInfo RegisterServiceInterface( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t != typeof( IAmbientService ) && t.IsInterface );
            if( !_serviceInterfaces.TryGetValue( t, out var info ) )
            {
                info = _typeFilter( _monitor, t)
                        ? new AmbientServiceInterfaceInfo( t, RegisterServiceInterfaces( t.GetInterfaces() ) )
                        : null;
                _serviceInterfaces.Add( t, info );
            }
            return info;
        }

        internal IEnumerable<AmbientServiceInterfaceInfo> RegisterServiceInterfaces( IEnumerable<Type> interfaces )
        {
            foreach( var iT in interfaces )
            {
                if( iT != typeof( IAmbientService )
                    && typeof(IAmbientService).IsAssignableFrom( iT ) )
                {
                    var r = RegisterServiceInterface( iT );
                    if( r != null ) yield return r;
                }
            }
        }


        AmbientServiceCollectorResult GetAmbientServiceResult( AmbientContractCollectorResult contracts )
        {
            var interfaces = GetLeafServiceInterfaces();
            List<AmbientServiceClassInfo> requiresUnification = null;
            var classes = GetLeafServiceClasses( contracts.EngineMap, ref requiresUnification );
            if( classes != null
                && requiresUnification != null
                && !PureClassUnification( classes, requiresUnification ) )
            {
                // Error!
                classes = null;
            }
            return new AmbientServiceCollectorResult( interfaces, classes, requiresUnification );
        }

        bool PureClassUnification( List<AmbientServiceClassInfo> classes, List<AmbientServiceClassInfo> requiresUnification )
        {
            Debug.Assert( classes.Count > 0 && requiresUnification.Count > 0 );
        }

        IReadOnlyList<AmbientServiceInterfaceInfo> GetLeafServiceInterfaces()
        {
            bool error = false;
            var baseServices = new List<AmbientServiceInterfaceInfo>();
            foreach( var iS in _serviceInterfaces.Values )
            {
                if( iS == null || iS.IsSpecialized ) continue;
                if( iS.CheckUnification( _monitor ) )
                {
                    baseServices.Add( iS );
                }
                else error = true;
            }
            return error ? null : baseServices;
        }

        List<AmbientServiceClassInfo> GetLeafServiceClasses( StObjObjectEngineMap engineMap, ref List<AmbientServiceClassInfo> requiresUnification )
        {
            bool error = false;
            var classes = new List<AmbientServiceClassInfo>();
            foreach( var c in _serviceCollector.Values )
            {
                if( c == null || c.IsExcluded ) continue;
                if( !c.IsSpecialized && !c.EnsureTypeImplementation( _monitor, _tempAssembly ) )
                {
                    _monitor.Error( $"Class '{c.Type.FullName}' is abstract and can not be automatically implemented." );
                    error = true;
                }
                else
                {
                    if( c.ContainerType != null && c.BindToContainerItem( _monitor, engineMap ) == null )
                    {
                        _monitor.Error( $"Unable to resolve container '{c.ContainerType.FullName}' for service '{c.Type.FullName}' to a StObj." );
                        error = true;
                    }
                    else
                    {
                        Debug.Assert( (c.ContainerType == null) == (c.ContainerItem == null) );
                        if( !c.IsSpecialized )
                        {
                            if( c.BindToSingleCtorParameters(_monitor, this ) )
                            {
                                classes.Add( c );
                            }
                            else error = true;
                        }
                        else if( c.SpecializationsCount > 1 )
                        {
                            if( requiresUnification == null ) requiresUnification = new List<AmbientServiceClassInfo>();
                            requiresUnification.Add( c );
                        }
                    }
                }
            }
            return error ? null : classes;
        }
    }


}
