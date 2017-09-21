using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly List<IStObjEngineAspectConfiguration> _aspects;

        /// <summary>
        /// The standard engine is "CK.Setup.StObjEngine, CK.StObj.Engine".
        /// </summary>
        static public readonly string StandardEngineTypeName = "CK.Setup.StObjEngine, CK.StObj.Engine";

        /// <summary>
        /// This current Xml schema version applies to <see cref="CK.Core.StObjEngineConfiguration"/> and
        /// its 3 parts: <see cref="CK.Core.BuildAndRegisterConfiguration"/>, <see cref="CK.Core.BuilderFinalAssemblyConfiguration"/>
        /// and the <see cref="Aspects"/>. This must not be used for aspects schemas: each aspect must implement its
        /// own versioning handling.
        /// </summary>
        public const int CurrentXmlVersion = 1;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public StObjEngineConfiguration()
        {
            _buildConfig = new BuildAndRegisterConfiguration();
            _finalConfig = new BuilderFinalAssemblyConfiguration();
            _aspects = new List<IStObjEngineAspectConfiguration>();
        }

        /// <summary>
        /// Defines Xml centralized names.
        /// </summary>
        public static class XmlNames
        {
            /// <summary>
            /// The version attribute name.
            /// </summary>
            static public readonly XName Version = XNamespace.None + "Version";

            /// <summary>
            /// The BuildAndRegisterConfiguration element name.
            /// </summary>
            static public readonly XName BuildAndRegisterConfiguration = XNamespace.None + "BuildAndRegisterConfiguration";

            /// <summary>
            /// The BuilderFinalAssemblyConfiguration element name.
            /// </summary>
            static public readonly XName BuilderFinalAssemblyConfiguration = XNamespace.None + "BuilderFinalAssemblyConfiguration";

            /// <summary>
            /// The StObjEngineConfiguration element name.
            /// </summary>
            static public readonly XName StObjEngineConfiguration = XNamespace.None + "StObjEngineConfiguration";

            /// <summary>
            /// The Aspect element name.
            /// </summary>
            static public readonly XName Aspect = XNamespace.None + "Aspect";

            /// <summary>
            /// The Type element name.
            /// </summary>
            static public readonly XName Type = XNamespace.None + "Type";

            /// <summary>
            /// The RevertOrderingNames element name.
            /// </summary>
            static public readonly XName RevertOrderingNames = XNamespace.None + "RevertOrderingNames";

            /// <summary>
            /// The TraceDependencySorterInput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterInput = XNamespace.None + "TraceDependencySorterInput";

            /// <summary>
            /// The TraceDependencySorterOutput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterOutput = XNamespace.None + "TraceDependencySorterOutput";

            /// <summary>
            /// The EngineAssemblyQualifiedName element name.
            /// </summary>
            static public readonly XName EngineAssemblyQualifiedName = XNamespace.None + "EngineAssemblyQualifiedName";
        }

        /// <summary>
        /// Initializes a new <see cref="StObjEngineConfiguration"/> from a <see cref="XElement"/>.
        /// </summary>
        /// <param name="e">The xml element.</param>
        /// <param name="aspectTypeResolver">
        /// Resolver for types.
        /// Defaults to a function that calls <see cref="SimpleTypeFinder.WeakResolver"/>.
        /// </param>
        public StObjEngineConfiguration( XElement e, Func<string, Type> aspectTypeResolver = null )
        {
            if( aspectTypeResolver == null )
            {
                aspectTypeResolver = aqn => SimpleTypeFinder.WeakResolver( aqn, true );
            }
            int? nv = (int?)e.Attribute( XmlNames.Version );
            int version = nv.HasValue ? nv.Value : CurrentXmlVersion;
            TraceDependencySorterInput = string.Equals( e.Element( XmlNames.TraceDependencySorterInput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            TraceDependencySorterOutput = string.Equals( e.Element( XmlNames.TraceDependencySorterOutput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            RevertOrderingNames = string.Equals( e.Element( XmlNames.RevertOrderingNames )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            EngineAssemblyQualifiedName = e.Element( XmlNames.EngineAssemblyQualifiedName )?.Value ?? StandardEngineTypeName;
            _buildConfig = new BuildAndRegisterConfiguration( e.Element( XmlNames.BuildAndRegisterConfiguration ), version );
            _finalConfig = new BuilderFinalAssemblyConfiguration( e.Element( XmlNames.BuilderFinalAssemblyConfiguration ), version );
            _aspects = new List<IStObjEngineAspectConfiguration>();
            foreach( var a in e.Elements( XmlNames.Aspect ) )
            {
                string type = (string)a.AttributeRequired( XmlNames.Type );
                Type tAspect = aspectTypeResolver( type );
                IStObjEngineAspectConfiguration aspect = (IStObjEngineAspectConfiguration)Activator.CreateInstance( tAspect, a );
                _aspects.Add( aspect );
            }
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The <see cref="StObjEngineConfiguration"/> constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <param name="aspectTypeNameWriter">
        /// Writer for aspects type names. 
        /// Defaults to a function that returns a weak assembly name from <see cref="Type.AssemblyQualifiedName"/>
        /// (using <see cref="SimpleTypeFinder.WeakenAssemblyQualifiedName(string, out string)"/>).
        /// </param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e, Func<Type, string> aspectTypeNameWriter = null )
        {
            if( aspectTypeNameWriter == null )
            {
                aspectTypeNameWriter = t =>
                {
                    SimpleTypeFinder.WeakenAssemblyQualifiedName( t.AssemblyQualifiedName, out string weaken );
                    return weaken;
                };
            }
            e.Add( new XAttribute( XmlNames.Version, CurrentXmlVersion ),
                   TraceDependencySorterInput ? new XElement( XmlNames.TraceDependencySorterInput, "true" ) : null,
                   TraceDependencySorterOutput ? new XElement( XmlNames.TraceDependencySorterOutput, "true" ) : null,
                   RevertOrderingNames ? new XElement( XmlNames.RevertOrderingNames, "true" ) : null,
                   EngineAssemblyQualifiedName != StandardEngineTypeName ? new XElement( XmlNames.EngineAssemblyQualifiedName, EngineAssemblyQualifiedName ) : null,
                   _buildConfig.SerializeXml( new XElement( XmlNames.BuildAndRegisterConfiguration ) ),
                   _finalConfig.SerializeXml( new XElement( XmlNames.BuilderFinalAssemblyConfiguration ) ),
                   _aspects.Select( a => a.SerializeXml( new XElement( XmlNames.Aspect, new XAttribute( XmlNames.Type, aspectTypeNameWriter( a.GetType() ) ) ) ) ) );
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
        /// Gets the list of all configuration aspects that must participate to setup.
        /// </summary>
        public List<IStObjEngineAspectConfiguration> Aspects => _aspects;

        /// <summary>
        /// Gets ors sets whether the ordering of StObj that share the same rank in the dependency graph must be inverted.
        /// Defaults to false.
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of IDependentItem) associated
        /// to the StObj objects must be send to the monitor before sorting.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterInput { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of ISortedItem) associated
        /// to the StObj objects must be send to the monitor once the graph is sorted.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterOutput { get; set; }

        /// <summary>
        /// Gets or sets the engine Assembly Qualified Name.
        /// Defaults to <see cref="StandardEngineTypeName"/>.
        /// Engine can be any object with a public constructor with parameters
        /// (<see cref="IActivityMonitor"/>, <see cref="StObjEngineConfiguration"/>, <see cref="IStObjRuntimeBuilder"/>)
        /// and  a public <c>bool Run()</c> method.
        /// </summary>
        public string EngineAssemblyQualifiedName { get; set; } = StandardEngineTypeName;

    }
}
