using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// This interface allows dynamic configuration of items.
    /// </summary>
    public interface IStObjExternalConfigurator
    {
        /// <summary>
        /// Enables configration of items before setup process.
        /// To remove a class from a setup <see cref="IAmbiantContractDispatcher"/> must be used.
        /// </summary>
        /// <param name="o">The item to configure.</param>
        void Configure( IStObjMutableItem o );
    }
}
