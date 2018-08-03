#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AmbientContract\AmbientTypeInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Encapsulate type information for an Ambient Contract or Service class.
    /// Offers persistent access to attributes that support <see cref="IAttributeAmbientContextBound"/> interface.
    /// Attributes must be retrieved thanks to <see cref="Attributes">.
    /// This type information are built top-down (from generalization to most specialized type).
    /// </summary>
    public class AmbientTypeInfo
    {
        readonly TypeAttributesCache _attributes;
        readonly AmbientTypeInfo _nextSibling;
        AmbientTypeInfo _firstChild;

        /// <summary>
        /// Initializes a new <see cref="AmbientTypeInfo"/> from a base one (its <see cref="Generalization"/>) if it exists and a type.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="t">Type itself. Can not be null.</param>
        /// <param name="parent">Parent AmbientTypeInfo (Generalization). Null if the base type is not an Ambient type.</param>
        /// <param name="services">Available services that will be used for delegated attribute constructor injection.</param>
        public AmbientTypeInfo( IActivityMonitor monitor, AmbientTypeInfo parent, Type t, IServiceProvider services )
        {
            _attributes = new TypeAttributesCache( monitor, t, services, parent == null );
            if( (Generalization = parent) == null )
            {
                _nextSibling = null;
            }
            else
            {
                _nextSibling = Generalization._firstChild;
                Generalization._firstChild = this;
            }
        }

        /// <summary>
        /// Gets the Type that is decorated.
        /// </summary>
        public Type Type => _attributes.Type;

        /// <summary>
        /// Gets the generalizatiuon of this <see cref="Type"/>.
        /// </summary>
        public AmbientTypeInfo Generalization { get; }

        /// <summary>
        /// Gets whether this Type (that is abstract) must actually be considered as an abstract type or not.
        /// An abstract class may be considered as concrete if there is a way to concretize an instance. 
        /// This must be called only for abstract types and if <paramref name="assembly"/> is not null.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="assembly">The dynamic assembly to use for generated types if necessary.</param>
        /// <returns>Concrete Type builder or null.</returns>
        protected ImplementableTypeInfo CreateAbstractTypeImplementation( IActivityMonitor monitor, IDynamicAssembly assembly )
        {
            Debug.Assert( Type.IsAbstract && assembly != null );

            List<ICKCustomAttributeProvider> combined = new List<ICKCustomAttributeProvider>();
            var p = this;
            do { combined.Add( p.Attributes ); p = p.Generalization; } while( p != null );

            ImplementableTypeInfo autoImpl = ImplementableTypeInfo.CreateImplementableTypeInfo( monitor, Type, new CustomAttributeProviderComposite( combined ) );
            if( autoImpl != null && autoImpl.CreateStubType( monitor, assembly ) != null )
            {
                return autoImpl;
            }
            return null;
        }

        /// <summary>
        /// Gets the provider for attributes. Attributes that are marked with <see cref="IAttributeAmbientContextBound"/> are cached
        /// and can keep an internal state if needed.
        /// </summary>
        /// <remarks>
        /// All attributes related to ObjectType (either on the type itself or on any of its members) should be retrieved 
        /// thanks to this method otherwise stateful attributes will not work correctly.
        /// </remarks>
        public ICKCustomAttributeTypeMultiProvider Attributes => _attributes;

        /// <summary>
        /// Gets the different specialized <see cref="AmbientTypeInfo"/>.
        /// </summary>
        /// <returns>An enumerable of <see cref="AmbientTypeInfo"/> that specialize this one.</returns>
        public IEnumerable<AmbientTypeInfo> Specializations
        {
            get
            {
                AmbientTypeInfo c = _firstChild;
                while( c != null )
                {
                    yield return c;
                    c = c._nextSibling;
                }
            }
        }

    }
}
