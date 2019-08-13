using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Marker interface for attributes so that they are bound to an Ambient type. Attributes instances are 
    /// cached: their lifecycle are then the same as the contextualized type information.
    /// </summary>
    public interface IAttributeAmbientContextBound
    {
    }
    
}
