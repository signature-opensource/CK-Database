using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// All configuration of a Setup engine Aspect must implement this interface.
    /// Such objects must have a deserialization constructor from a XElement and should 
    /// avoid any <see cref="Type"/> or delegates of any kind.
    /// </summary>
    public interface ISetupEngineAspectConfiguration
    {
        /// <summary>
        /// Gets the fully qualified name of the class that implements this aspect.
        /// </summary>
        string AspectType { get; }

    }
}
