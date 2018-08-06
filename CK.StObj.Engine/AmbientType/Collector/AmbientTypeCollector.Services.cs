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
        readonly List<AmbientServiceClassInfo> _serviceRoots;
        readonly Dictionary<Type, AmbientServiceInterfaceInfo> _serviceInterfaces;

        AmbientServiceClassInfo RegisterServiceClass( Type t, AmbientServiceClassInfo parent )
        {
            var serviceInfo = new AmbientServiceClassInfo( _monitor, _serviceProvider, parent, t, this, !_typeFilter( t ) );
            if( !serviceInfo.IsExcluded )
            {
                RegisterAssembly( t );
                if( parent == null ) _serviceRoots.Add( serviceInfo );
            }
            _serviceCollector.Add( t, serviceInfo );
            return serviceInfo;
        }

        internal AmbientServiceClassInfo RegisterCtorDepClass( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t.IsClass );
            DoRegisterClass( t, out _, out var info );
            return info;
        }

        internal AmbientServiceInterfaceInfo RegisterServiceInterface( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t != typeof( IAmbientService ) && t.IsInterface );
            if( !_serviceInterfaces.TryGetValue( t, out var info ) )
            {
                info = new AmbientServiceInterfaceInfo( t, RegisterServiceInterfaces( t.GetInterfaces() ) );
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
                    
                    yield return RegisterServiceInterface( iT );
                }
            }
        }

    }


}
