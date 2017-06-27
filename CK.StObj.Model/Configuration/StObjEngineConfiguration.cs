using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates configuration for the StObj layer itself.
    /// </summary>
    public sealed class StObjEngineConfiguration
    {
        readonly BuildAndRegisterConfiguration _buildConfig;
        readonly BuilderFinalAssemblyConfiguration _finalConfig;

        /// <summary>
        /// This current Xml schema version applies to <see cref="CK.Core.StObjEngineConfiguration"/> and
        /// its 2 parts: <see cref="CK.Core.BuildAndRegisterConfiguration"/> 
        /// and <see cref="CK.Core.BuilderFinalAssemblyConfiguration"/>.
        /// </summary>
        public const int CurrentXmlVersion = 1;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public StObjEngineConfiguration()
        {
            _buildConfig = new BuildAndRegisterConfiguration();
            _finalConfig = new BuilderFinalAssemblyConfiguration();
        }

        static readonly XName xBuildAndRegisterConfiguration = XNamespace.None + "BuildAndRegisterConfiguration";
        static readonly XName xBuilderFinalAssemblyConfiguration = XNamespace.None + "BuilderFinalAssemblyConfiguration";
        public static readonly XName xVersion = XNamespace.None + "Version";

        /// <summary>
        /// Initializes a new <see cref="StObjEngineConfiguration"/> from a <see cref="XElement"/>.
        /// </summary>
        /// <param name="e">The xml element.</param>
        public StObjEngineConfiguration( XElement e )
        {
            int? nv = (int?)e.Attribute( xVersion );
            int v = nv.HasValue ? nv.Value : CurrentXmlVersion;
            _buildConfig = new BuildAndRegisterConfiguration( e.Element( xBuildAndRegisterConfiguration ), v );
            _finalConfig = new BuilderFinalAssemblyConfiguration( e.Element( xBuilderFinalAssemblyConfiguration ), v );
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The <see cref="StObjEngineConfiguration(XElement)"/> constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( new XAttribute( xVersion, CurrentXmlVersion ),
                   _buildConfig.SerializeXml( new XElement( xBuildAndRegisterConfiguration ) ),
                   _finalConfig.SerializeXml( new XElement( xBuilderFinalAssemblyConfiguration ) ) );
            return e;
        }

        /// <summary>
        /// Gets the configuration that describes how Application Domain must be used during build and
        /// which assemlies and types must be discovered.
        /// </summary>
        public BuildAndRegisterConfiguration BuildAndRegisterConfiguration => _buildConfig;

        /// <summary>
        /// Gets the configuration related to final assembly generation.
        /// </summary>
        public BuilderFinalAssemblyConfiguration FinalAssemblyConfiguration => _finalConfig;

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of IDependentItem) must be send to the monitor before sorting.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterInput { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of ISortedItem) must be send to the monitor once the graph is sorted.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterOutput { get; set; }

    }
}
