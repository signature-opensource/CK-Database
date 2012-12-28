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
        /// Creates an <see cref="IMutableSetupItemContainer"/> from a <see cref="IStObjSetupData"/>.
        /// Returning null here triggers an attempt to instantiate an object of the type <see cref="IStObjSetupData.ItemType"/>
        /// with the same parameters as this method (the logger and the data). If no <see cref="IStObjSetupData.ItemType"/> is set,
        /// a <see cref="StObjDynamicPackageItem"/> is instanciated.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="data">Descriptive data (initialized by attributes and other direct code-first approaches and configured by any <see cref="IStObjSetupConfigurator"/>).</param>
        /// <returns>A <see cref="IMutableSetupItem"/> implementation that must be correctly initialized by the given data, or null to use the default <see cref="StObjDynamicPackageItem"/>.</returns>
        IMutableSetupItem CreateDependentItem( IActivityLogger logger, IStObjSetupData data );
    }

}
