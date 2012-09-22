using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Enables configuration of <see cref="IMutableStObjSetupData"/> before setup process.
    /// </summary>
    public interface IStObjSetupConfigurator
    {
        /// <summary>
        /// Configures the given <see cref="IMutableStObjSetupData"/> before it participates in setup.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="data">Mutable data (typically initialized by attributes and other direct code-first approaches).</param>
        void ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data );
    }

}
