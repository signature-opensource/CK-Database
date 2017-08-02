using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    /// <summary>
    /// Collects changes from <see cref="ComponentDB"/> mutations.
    /// </summary>
    public interface IComponentDBEventSink
    {
        /// <summary>
        /// Called when files have been removed from a registered Component.
        /// </summary>
        /// <param name="cRef">The Component.</param>
        /// <param name="files">Files removed (potentially more than actual files).</param>
        void FilesRemoved( Component c, IReadOnlyList<string> files );

        /// <summary>
        /// Called whenever a new Component has been added.
        /// </summary>
        /// <param name="c">The new Component.</param>
        /// <param name="f">Origin folder.</param>
        void ComponentLocallyAdded( Component c, BinFolder f );

        /// <summary>
        /// Called whenever a Component has been removed.
        /// </summary>
        /// <param name="c">Component removed.</param>
        void ComponentRemoved( Component c );

        /// <summary>
        /// Called whenever a file has been imported.
        /// </summary>
        /// <param name="c">The component.</param>
        /// <param name="fileName">The file name without entry path.</param>
        void FileImported( Component c, string fileName );
    }
}
