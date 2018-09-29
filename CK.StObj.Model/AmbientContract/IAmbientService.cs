using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface marker states that a class or an interface instance
    /// must be a unique Service in a context.
    /// It is not required to be this exact type: any empty interface (no members)
    /// named "IAmbientService" defined in any namespace will be considered as
    /// a valid marker.
    /// </summary>
    public interface IAmbientService
    {
    }

}
