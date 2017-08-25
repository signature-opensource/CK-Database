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
        /// Defines Xml centralized names.
        /// </summary>
        public static class XmlNames
        {
            /// <summary>
            /// The TraceDependencySorterInput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterInput = XNamespace.None + "TraceDependencySorterInput";

            /// <summary>
            /// The TraceDependencySorterOutput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterOutput = XNamespace.None + "TraceDependencySorterOutput";

            /// <summary>
            /// The RevertOrderingNames element name.
            /// </summary>
            static public readonly XName RevertOrderingNames = XNamespace.None + "RevertOrderingNames";

        }

        /// <summary>
        /// Reads a <see cref="XElement"/> content (typically created by <see cref="SerializeXml(XElement, Func{Type, string})"/>).
        /// </summary>
        /// <param name="e">The element to read.</param>
        /// <param name="aspectTypeResolver">
        /// Resolver for types.
        /// Defaults to a function that calls <see cref="SimpleTypeFinder.WeakResolver"/>.
        /// </param>
        public SetupableAspectConfiguration( XElement e )
        {
            int? nv = (int?)e.Attribute( StObjEngineConfiguration.XmlNames.Version );
            int v = nv.HasValue ? nv.Value : CurrentXmlVersion;
            TraceDependencySorterInput = string.Equals( e.Element( XmlNames.TraceDependencySorterInput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            TraceDependencySorterOutput = string.Equals( e.Element( XmlNames.TraceDependencySorterOutput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            RevertOrderingNames = string.Equals( e.Element( XmlNames.RevertOrderingNames )?.Value, "true", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( new XAttribute( StObjEngineConfiguration.XmlNames.Version, CurrentXmlVersion ),
                   RevertOrderingNames ? new XElement( XmlNames.RevertOrderingNames, "true" ) : null,
                   TraceDependencySorterInput ? new XElement( XmlNames.TraceDependencySorterInput, "true" ) : null,
                   TraceDependencySorterOutput ? new XElement( XmlNames.TraceDependencySorterOutput, "true" ) : null );
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
        /// Gets the 3 steps setup aspect engine Assmbly Qualified Name: "CK.Setup.SetupableAspect, CK.Setupable.Engine"
        /// </summary>
        public string AspectType => "CK.Setup.SetupableAspect, CK.Setupable.Engine";
    }
}
