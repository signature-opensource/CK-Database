using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Collects <see cref="ISetupScript">scripts</see> originating from multiple sources.
    /// </summary>
    public interface IScriptCollector
    {
        /// <summary>
        /// Registers a <see cref="ISetupScript"/>: finds or creates a unique set of scripts for each <see cref="ISetupScript.Name"/>.
        /// The first added name becomes the case-insensitive key: names with different case will
        /// be detected, a warning will be emitted into the logger and false will be returned.
        /// </summary>
        /// <param name="script">A setup script. Must be not null.</param>
        /// <param name="logger">The logger to use.</param>
        /// <returns>False if casing differ or if the script already exists in the set of scripts for <see cref="ISetupScript.Name"/>.</returns>
        bool Add( ISetupScript script, IActivityLogger logger );


        /// <summary>
        /// Registers a set of resources (multiple <see cref="ResSetupScript"/>) from a <see cref="ResourceLocator"/>, a full name prefix and a script source.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="scriptSource">The script source under which registering the <see cref="ISetupScript"/>.</param>
        /// <param name="resLoc">Resource locator.</param>
        /// <param name="context">Context identifier.</param>
        /// <param name="location">Location identifier.</param>
        /// <param name="name">Name of the object. This is used as a prefix for the resource names.</param>
        /// <param name="fileSuffix">Keeps only resources that ends with this suffix.</param>
        /// <returns>The number of scripts that have been added.</returns>
        int AddFromResources( IActivityLogger logger, string scriptSource, ResourceLocator resLoc, string context, string location, string name, string fileSuffix );

    }
}
