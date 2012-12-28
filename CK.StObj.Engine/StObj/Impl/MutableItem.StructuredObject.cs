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
        protected internal override bool AbstractTypeCanBeInstanciated( IActivityLogger logger, DynamicAssembly assembly )
        {
            Debug.Assert( Specialization == null && Type.IsAbstract );
            Debug.Assert( _leafData.ImplementableTypeInfo == null, "Only called once." );
            _leafData.ImplementableTypeInfo = ImplementableTypeInfo.CreateImplementableTypeInfo( logger, Type, this );
            if( _leafData.ImplementableTypeInfo != null )
            {
                return _leafData.ImplementableTypeInfo.CreateTypeFromCurrent( logger, assembly ) != null;
            }
            return false;
        }

        public object CreateStructuredObject( IActivityLogger logger )
        {
            Debug.Assert( Specialization == null );
            Debug.Assert( _leafData.StructuredObject == null, "Called once and only once." );
            try
            {
                Type toInstanciate = _leafData.ImplementableTypeInfo != null ? _leafData.ImplementableTypeInfo.LastGeneratedType : Type;
                return _leafData.StructuredObject = Activator.CreateInstance( toInstanciate );
            }
            catch( Exception ex )
            {
                logger.Error( ex );
                return null;
            }
        }

        public void CreateFinalType( IActivityLogger logger, DynamicAssembly a )
        {
            Debug.Assert( Specialization == null );
            if( _leafData.ImplementableTypeInfo != null ) _leafData.ImplementableTypeInfo.CreateFinalType( logger, a, true );
        }
    }
}
