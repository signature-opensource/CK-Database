using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Marker attribute (alternative to the <see cref="ISqlCallContext"/> interface marker) that 
    /// tags classes that hold contextual parameters
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, Inherited = true )]
    public sealed class SqlCallContextAttribute : Attribute
    {
    }
}
