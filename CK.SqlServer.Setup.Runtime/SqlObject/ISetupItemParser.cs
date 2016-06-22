using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.Setup
{
    /// <summary>
    /// Factory of setup items from text content.
    /// This is not Sql Server specific.
    /// </summary>
    public interface ISetupItemParser
    {
        /// <summary>
        /// Creates a <see cref="ISetupItem"/> from text content.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="itemName">Name of the item.</param>
        /// <param name="parser">The parser to use.</param>
        /// <param name="text">The test to parse.</param>
        /// <param name="fileName">The file name to use in traces.</param>
        /// <param name="packageItem">Optional package that defines this item if known.</param>
        /// <param name="expectedItemTypes">Optional restrictions of expected item types.</param>
        /// <returns>A new <see cref="ISetupItem"/> or null on error.</returns>
        ISetupItem Create( IActivityMonitor monitor, IContextLocNaming itemName, string text, string fileName, IDependentItemContainer package = null, IEnumerable<string> expectedItemTypes = null );
    }
}
