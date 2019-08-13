using System;
using System.Collections.Generic;
using CK.Core;
using System.Xml.Linq;
using System.Linq;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Configuration of 3 steps setup aspect.
    /// </summary>
    public class SetupableAspectConfiguration : IStObjEngineAspectConfiguration
    {
        /// <summary>
        /// Initializes a new empty <see cref="SetupableAspectConfiguration"/>.
        /// </summary>
        public SetupableAspectConfiguration()
        {
        }

        /// <summary>
        /// The current Xml schema version.
        /// </summary>
        public const int CurrentXmlVersion = 1;

        /// <summary>
        /// The KeepUnaccessedItemsVersion element name.
        /// </summary>
        static public readonly XName xKeepUnaccessedItemsVersion = XNamespace.None + "KeepUnaccessedItemsVersion";

        /// <summary>
        /// Reads a <see cref="XElement"/> content (typically created by <see cref="SerializeXml(XElement)"/>).
        /// </summary>
        /// <param name="e">The element to read.</param>
        public SetupableAspectConfiguration( XElement e )
        {
            int v = (int?)e.Attribute( StObjEngineConfiguration.xVersion ) ?? CurrentXmlVersion;
            TraceDependencySorterInput = (bool?)e.Element( StObjEngineConfiguration.xTraceDependencySorterInput ) ?? false;
            TraceDependencySorterOutput = (bool?)e.Element( StObjEngineConfiguration.xTraceDependencySorterOutput ) ?? false;
            RevertOrderingNames = (bool?)e.Element( StObjEngineConfiguration.xRevertOrderingNames ) ?? false;
            KeepUnaccessedItemsVersion = (bool?)e.Element( xKeepUnaccessedItemsVersion ) ?? false;
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( new XAttribute( StObjEngineConfiguration.xVersion, CurrentXmlVersion ),
                   RevertOrderingNames ? new XElement( StObjEngineConfiguration.xRevertOrderingNames, true ) : null,
                   TraceDependencySorterInput ? new XElement( StObjEngineConfiguration.xTraceDependencySorterInput, true ) : null,
                   TraceDependencySorterOutput ? new XElement( StObjEngineConfiguration.xTraceDependencySorterOutput, true ) : null,
                   KeepUnaccessedItemsVersion ? new XElement( xKeepUnaccessedItemsVersion, true ) : null );
            return e;
        }

        /// <summary>
        /// Gets or sets whether ordering for setupable items that share the same rank 
        /// in the dependency graph is inverted. (See DependencySorter object in CK.Setup.Dependency assembly for more information.)
        /// Defaults to false.
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of IDependentItem) associated to the
        /// setup items must be send to the monitor before sorting.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterInput { get; set; }

        /// <summary>
        /// Gets whether the dependency graph (the set of ISortedItem)  associated to the
        /// setup items must be send to the monitor once the graph is sorted.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterOutput { get; set; }

        /// <summary>
        /// Gets whether version of items that have not been accessed during the setup are
        /// removed from the version tracking store (whatever its implementation is).
        /// Defaults to false.
        /// </summary>
        public bool KeepUnaccessedItemsVersion { get; set; }

        /// <summary>
        /// Gets the 3 steps setup aspect engine Assmbly Qualified Name: "CK.Setup.SetupableAspect, CK.Setupable.Engine"
        /// </summary>
        public string AspectType => "CK.Setup.SetupableAspect, CK.Setupable.Engine";
    }
}
