using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// This interface allows dynamic configuration of items.
    /// It can be supported by attributes (to be aplied on Structured Object type or on its members) or be 
    /// used globally as a configuration of <see cref="StObjCollector"/> object (an instance can be passed 
    /// as a parameter to the constructor of StObjCollector).
    /// </summary>
    public interface IStObjStructuralConfigurator
    {
        /// <summary>
        /// Enables configuration of items before setup process.
        /// To remove a class from a setup, <see cref="IAmbientContractDispatcher"/> must be used.
        /// </summary>
        /// <param name="o">The item to configure.</param>
        void Configure( IActivityMonitor monitor, IStObjMutableItem o );
    }
}
