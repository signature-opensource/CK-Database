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
    public class AmbientContextualTypeInfo<T,TC> : AmbientContextualAttributesCache
        where T : AmbientTypeInfo
        where TC : AmbientContextualTypeInfo<T, TC>
    {
        readonly TC _specialization;
        TC _generalization;

        /// <summary>
        /// Initializes a new <see cref="AmbientContextualTypeInfo"/>. 
        /// Attributes must be retrieved with <see cref="AmbientContextualAttributesCache.GetCustomAttributes">GetCustomAttributes</see> methods.
        /// </summary>
        /// <param name="t">Type.</param>
        /// <param name="context">Context.</param>
        /// <param name="specialization">Specialization in this context. Null if this is the leaf of the specialization path.</param>
        /// <remarks>
        /// Contextual type information are built bottom up (from most specialized type to generalization).
        /// </remarks>
        internal protected AmbientContextualTypeInfo( T t, IAmbientContextualTypeMap context, TC specialization )
            : base( t.Type )
        {
            Debug.Assert( t != null );
            AmbientTypeInfo = t;
            Context = context;
            _specialization = specialization;
            if( _specialization != null )
            {
                _specialization._generalization = (TC)this;
            }
        }

        /// <summary>
        /// Contextless type information.
        /// </summary>
        public readonly T AmbientTypeInfo;

        /// <summary>
        /// Context of this contextual type information.
        /// </summary>
        public IAmbientContextualTypeMap Context { get; private set; }

        /// <summary>
        /// Gets the specialization in this <see cref="Context"/>. 
        /// Null if this is the leaf of the specialization path.
        /// </summary>
        public TC Specialization { get { return _specialization; } }

        /// <summary>
        /// Gets the generalization in this <see cref="Context"/>. 
        /// Null if this is the root of the specialization path.
        /// </summary>
        public TC Generalization { get { return _generalization; } }

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

        internal List<TC> CreatePathType( List<TC> path )
        {
            if( AmbientTypeInfo.Generalization != null ) AmbientTypeInfo.Generalization.CreateContextTypeInfo<T,TC>( Context, (TC)this ).CreatePathType( path );
            path.Add( (TC)this );
            return path;
        }

        public override string ToString()
        {
            return AmbientContractCollector.FormatContextualFullName( Context.Context, Type );
        }
    }

}
