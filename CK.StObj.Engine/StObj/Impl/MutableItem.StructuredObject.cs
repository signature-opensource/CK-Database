#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\MutableItem.StructuredObject.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;
using System.Reflection;

namespace CK.Setup
{

    partial class MutableItem
    {
        protected internal override bool AbstractTypeCanBeInstanciated( IActivityMonitor monitor, DynamicAssembly assembly, out object abstractTypeInfo )
        {
            Debug.Assert(assembly != null );
            Debug.Assert(Specialization == null && Type.IsAbstract);

            List<ICKCustomAttributeProvider> combined = new List<ICKCustomAttributeProvider>();
            var p = this;
            do { combined.Add( p ); p = p.Generalization; } while( p != null );

            ImplementableTypeInfo autoImpl = ImplementableTypeInfo.CreateImplementableTypeInfo( monitor, Type.AsType(), new CustomAttributeProviderComposite( combined ) );
            if( autoImpl != null && autoImpl.CreateStubType( monitor, assembly ) != null )
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
                return _leafData.CreateStructuredObject( runtimeBuilder, ObjectType );
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex );
                return null;
            }
        }
#if NET461
        public Type CreateFinalType( IActivityMonitor monitor, DynamicAssembly a )
        {
            Debug.Assert( Specialization == null );
            return _leafData.ImplementableTypeInfo == null 
                        ? Type 
                        : _leafData.ImplementableTypeInfo.CreateFinalType( monitor, a );
        }
#endif

        /// <summary>
        /// Gets the properties to set right before the call to StObjConstruct.
        /// Properties are registered at the root object, the Property.DeclaringType can be used to
        /// target the correct type in the inheritance chain.
        /// </summary>
        public IReadOnlyList<PropertySetter> PreConstructProperties => _preConstruct;

        public string GetFinalTypeFullName( IActivityMonitor monitor, IDynamicAssembly a )
        {
            Debug.Assert( Specialization == null );
            return _leafData.ImplementableTypeInfo == null
                        ? ObjectType.AssemblyQualifiedName 
                        : _leafData.ImplementableTypeInfo.GenerateType( monitor, a );
        }

        /// <summary>
        /// Gets the post build properties to set. Potentially not null only on leaves.
        /// </summary>
        public IReadOnlyList<PropertySetter> PostBuildProperties => _leafData?.PostBuildProperties;


    }
}
