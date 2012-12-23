using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Marker interface for attributes so that they are bound to an Ambient type. Attributes instances are 
    /// cached by <see cref="AmbientContextTypeInfo"/>: their lifecycle can then be synchronized 
    /// with the contextualized type information.
    /// </summary>
    public interface IAttributeAmbientContextBound
    {
        void Initialize( MemberInfo i );
    }
    
}
