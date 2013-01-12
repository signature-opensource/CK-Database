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
        protected internal override bool AbstractTypeCanBeInstanciated( IActivityLogger logger, DynamicAssembly assembly, out object abstractTypeInfo )
        {
            Debug.Assert( Specialization == null && Type.IsAbstract );

            List<ICustomAttributeProvider> combined = new List<ICustomAttributeProvider>();
            var p = this;
            do { combined.Add( p ); p = p.Generalization; } while( p != null );

            ImplementableTypeInfo autoImpl = ImplementableTypeInfo.CreateImplementableTypeInfo( logger, Type, new CustomAttributeProviderComposite( combined ) );
            if( autoImpl != null && autoImpl.CreateTypeFromCurrent( logger, assembly ) != null )
            {
                abstractTypeInfo = autoImpl;
                return true;
            }
            abstractTypeInfo = null;
            return false;
        }


        public object CreateStructuredObject( IActivityLogger logger )
        {
            Debug.Assert( Specialization == null );
            Debug.Assert( _leafData.StructuredObject == null, "Called once and only once." );
            try
            {
                Type toInstanciate = _leafData.ImplementableTypeInfo != null ? _leafData.ImplementableTypeInfo.LastGeneratedType : Type;
                return _leafData.StructuredObject = Activator.CreateInstance( toInstanciate, true );
            }
            catch( Exception ex )
            {
                logger.Error( ex );
                return null;
            }
        }

        public Type CreateFinalType( IActivityLogger logger, DynamicAssembly a )
        {
            Debug.Assert( Specialization == null );
            return _leafData.ImplementableTypeInfo == null ? Type : _leafData.ImplementableTypeInfo.CreateFinalType( logger, a, storeAsLastGeneratedType: false );
        }
    }
}
