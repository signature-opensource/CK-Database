using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace CK.Core
{

    /// <summary>
    /// Contextual type information: it is an <see cref="AmbientTypeInfo"/> inside a <see cref="Context"/>.
    /// Offers persistent access to attributes that support <see cref="IAttributeAmbientContextBound"/> interface.
    /// </summary>
    public class AmbientContextTypeInfo<T> : AmbientContextAttributesCache
        where T : AmbientTypeInfo
    {
        readonly AmbientContextTypeInfo<T> _specialization;

        /// <summary>
        /// Initializes a new <see cref="AmbientContextTypeInfo"/>. 
        /// Attributes must be retrieved with <see cref="AmbientContextAttributesCache.GetCustomAttributes">GetCustomAttributes</see> methods.
        /// </summary>
        /// <param name="t">Type.</param>
        /// <param name="context">Context name.</param>
        /// <param name="specialization">Specialization in this context. Null if this is the leaf of the specialization path.</param>
        /// <remarks>
        /// Contextual type information are built bottom up (from most specialized type to generalization).
        /// </remarks>
        internal protected AmbientContextTypeInfo( T t, string context, AmbientContextTypeInfo<T> specialization )
            : base( t.Type )
        {
            Debug.Assert( t != null && context != null );
            AmbientTypeInfo = t;
            Context = context;
            _specialization = specialization;
        }

        /// <summary>
        /// Contextless type information.
        /// </summary>
        public readonly T AmbientTypeInfo;

        /// <summary>
        /// Context name of this contextual type information.
        /// </summary>
        public readonly string Context;

        /// <summary>
        /// Gets the specialization in this <see cref="Context"/>. 
        /// Null if this is the leaf of the specialization path.
        /// </summary>
        /// <remarks>
        /// Masking (the C# new keyword) should be used on specialization to offer covariance for this property.
        /// </remarks>
        protected AmbientContextTypeInfo<T> Specialization { get { return _specialization; } }

        /// <summary>
        /// Gets whether this Type (that is abstract) must actually be considered as an abstract type or not.
        /// An abstract class may be considered as concrete if there is a way to concretize an instance. 
        /// This is called only for abstract types and if <paramref name="assembly"/> is not null.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="assembly">The dynamic assembly to use for generated types if necessary.</param>
        internal protected virtual bool AbstractTypeCanBeInstanciated( IActivityLogger logger, DynamicAssembly assembly )
        {
            Debug.Assert( AmbientTypeInfo.Type.IsAbstract && assembly != null );
            return false;
        }

        internal List<TC> CreatePathType<TC>( List<TC> path )
            where TC : AmbientContextTypeInfo<T>
        {
            if( AmbientTypeInfo.Generalization != null ) AmbientTypeInfo.Generalization.CreateContextTypeInfo<T,TC>( Context, (TC)this ).CreatePathType( path );
            path.Add( (TC)this );
            return path;
        }

        public override string ToString()
        {
            return AmbientContractCollector.FormatContextualFullName( Context, Type );
        }
    }

}
