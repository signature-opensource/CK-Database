using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Unifies <see cref="AmbientPropertyOrContractInfo"/> and <see cref="StObjPropertyInfo"/>.
    /// </summary>
    internal interface INamedPropertyInfo
    {
        string Name { get; }

        Type DeclaringType { get; }

        string Kind { get; }
    }
}
