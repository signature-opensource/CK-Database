using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Marker interface that extends <see cref="IAttributeAmbientContextBound"/> in order to 
    /// be initialized with the <see cref="MemberInfo"/> that is decorated with the attribute.
    /// </summary>
    public interface IAttributeAmbientContextBoundWithMember : IAttributeAmbientContextBound
    {
        /// <summary>
        /// Called the first time the attribute is obtained.
        /// </summary>
        /// <param name="i">The member that is decorated by this attribute.</param>
        void Initialize( MemberInfo i );
    }
    
}
