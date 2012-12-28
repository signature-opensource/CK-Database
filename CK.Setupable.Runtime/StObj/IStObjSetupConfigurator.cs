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
    /// <remarks>
    /// This interface can be implemented by different kind of objects:
    /// <list type="number">
    /// <item>
    ///     <term>On an Attribute that is applied to a StObj class.</term>
    ///     <description>Its <see cref="ConfigureDependentItem"/> will be called right after the object instanciation.</description>
    /// </item>
    /// <item>
    ///     <term>On the StObj class itself.</term>
    ///     <description>Its <see cref="ConfigureDependentItem"/> will be called after the ones of the attributes.</description>
    /// </item>
    /// <item>
    ///     <term>As a parameter to the <see cref="StObjSetupBuilder"/> .</term>
    ///     <description>Its <see cref="ConfigureDependentItem"/> will be called last for all StObj beeing setup.</description>
    /// </item>
    /// </list>
    /// </remarks>
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
