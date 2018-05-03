using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// When applied on an abstract class, prevents any kind of auto implementation for this 
    /// exact type: this is a kind of "really abstract" marker for implementable abstract classes. 
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class PreventAutoImplementationAttribute : Attribute
    {
    }
}


