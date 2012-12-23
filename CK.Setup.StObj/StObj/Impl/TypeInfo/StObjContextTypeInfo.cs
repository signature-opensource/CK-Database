using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Bridge between the AmbientContract base world and the <see cref="MutableItem"/> world.
    /// Specialized <see cref="StObjTypeInfo"/> directly instanciates <see cref="MutableItem"/> that inherits 
    /// from this intermediate class.
    /// </summary>
    /// <remarks>
    /// This could be removed in favor of sole MutableItem class, but I prefer introducing it for a better (if possible)
    /// comprehension of the architecture.
    /// </remarks>
    internal abstract class StObjContextTypeInfo : AmbientContextTypeInfo<StObjTypeInfo>
    {
        internal StObjContextTypeInfo( StObjTypeInfo t, string context, StObjContextTypeInfo specialization )
            : base( t, context, specialization )
        {
        }

        /// <summary>
        /// Used only for Empty Object Pattern implementations.
        /// </summary>
        protected StObjContextTypeInfo()
            : base( StObjTypeInfo.Empty, String.Empty, null )
        {
        }

        /// <summary>
        /// Gets the provider for attributes. Attributes that are marked with <see cref="IAttributeAmbientContextBound"/> are cached
        /// and can keep an internal state if needed.
        /// </summary>
        /// <remarks>
        /// All attributes related to <see cref="ObjectType"/> (either on the type itself or on any of its members) should be retrieved 
        /// thanks to this method otherwise stateful attributes may not work correctly.
        /// </remarks>
        public ICustomAttributeTypeProvider Attributes 
        {
            get { return this; } 
        }


    }
}
