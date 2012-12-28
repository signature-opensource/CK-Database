using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// When applied on an abstract class, prevents any <see cref="IAutoImplementorMethod"/> (or other similar mechanism).
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class PreventAutoImplementationAttribute : Attribute
    {
    }
}
