using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{

    partial class MutableItem
    {
        protected internal override bool AbstractTypeCanBeInstanciated( IActivityMonitor monitor, DynamicAssembly assembly, out object abstractTypeInfo )
        {
            Debug.Assert( Specialization == null && Type.IsAbstract );

            List<ICustomAttributeProvider> combined = new List<ICustomAttributeProvider>();
            var p = this;
            do { combined.Add( p ); p = p.Generalization; } while( p != null );

            ImplementableTypeInfo autoImpl = ImplementableTypeInfo.CreateImplementableTypeInfo( monitor, Type, new CustomAttributeProviderComposite( combined ) );
            if( autoImpl != null && autoImpl.CreateTypeFromCurrent( monitor, assembly ) != null )
            {
                abstractTypeInfo = autoImpl;
                return true;
            }
            abstractTypeInfo = null;
            return false;
        }


        public object CreateStructuredObject( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder )
        {
            Debug.Assert( Specialization == null );
            Debug.Assert( _leafData.StructuredObject == null, "Called once and only once." );
            try
            {
                return _leafData.CreateStructuredObject( runtimeBuilder, Type );
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex );
                return null;
            }
        }

        public Type CreateFinalType( IActivityMonitor monitor, DynamicAssembly a )
        {
            Debug.Assert( Specialization == null );
            return _leafData.ImplementableTypeInfo == null ? Type : _leafData.ImplementableTypeInfo.CreateFinalType( monitor, a, storeAsLastGeneratedType: false );
        }

        public void InjectFinalObjectAccessor( StObjContextRoot finalMapper )
        {
            Debug.Assert( Specialization == null );
            var ctx = finalMapper.FindContext( Context.Context );
            _leafData.StructuredObjectFunc = () => ctx.Obtain( Type );
        }
    }
}
