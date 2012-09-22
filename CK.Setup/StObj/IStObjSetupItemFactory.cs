using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Factory for items associated to <see cref="IStObj"/>.
    /// </summary>
    public interface IStObjSetupItemFactory
    {
        /// <summary>
        /// Creates an <see cref="IMutableDependentItem"/> from a <see cref="IStObjSetupData"/>.
        /// Returning null here triggers an attempt to instantiate an object of the type <see cref="IStObjSetupData.ItemType"/>
        /// with the same parameters as this method (the logger and the data). If no <see cref="IStObjSetupData.ItemType"/> is set,
        /// a <see cref="StObjDynamicPackageItem"/> is instanciated.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="data">Descriptive data (initialized by attributes and other direct code-first approaches and configured by any <see cref="IStObjSetupConfigurator"/>).</param>
        /// <returns>A <see cref="IMutableDependentItem"/> implementation that must be correctly initialized by the given data.</returns>
        IMutableDependentItem CreateItem( IActivityLogger logger, IStObjSetupData data );
    }

}
