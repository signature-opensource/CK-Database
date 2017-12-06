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
        /// <summary>
        /// Default assembly name.
        /// </summary>
        public const string DefaultGeneratedAssemblyName = "CK.StObj.AutoAssembly";

        readonly BuildAndRegisterConfiguration _buildConfig;
        readonly List<IStObjEngineAspectConfiguration> _aspects;
        string _generatedAssemblyName;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public StObjEngineConfiguration()
        {
            GenerateAppContextAssembly = true;
            _buildConfig = new BuildAndRegisterConfiguration();
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
            /// The GenerateAppContextAssembly element name.
            /// </summary>
            static public readonly XName GenerateAppContextAssembly = XNamespace.None + "GenerateAppContextAssembly";

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
        public StObjEngineConfiguration( XElement e )
        {
            TraceDependencySorterInput = string.Equals( e.Element( XmlNames.TraceDependencySorterInput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            TraceDependencySorterOutput = string.Equals( e.Element( XmlNames.TraceDependencySorterOutput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            RevertOrderingNames = string.Equals( e.Element( XmlNames.RevertOrderingNames )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            GenerateAppContextAssembly = !string.Equals( e.Element( XmlNames.GenerateAppContextAssembly )?.Value, "false", StringComparison.OrdinalIgnoreCase );

            _buildConfig = new BuildAndRegisterConfiguration( e.Element( XmlNames.BuildAndRegisterConfiguration ), 1 );
            _aspects = new List<IStObjEngineAspectConfiguration>();
            foreach( var a in e.Elements( XmlNames.Aspect ) )
            {
                string type = (string)a.AttributeRequired( XmlNames.Type );
                Type tAspect = SimpleTypeFinder.WeakResolver( type, true );
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
            e.Add( TraceDependencySorterInput ? new XElement( XmlNames.TraceDependencySorterInput, "true" ) : null,
                   TraceDependencySorterOutput ? new XElement( XmlNames.TraceDependencySorterOutput, "true" ) : null,
                   RevertOrderingNames ? new XElement( XmlNames.RevertOrderingNames, "true" ) : null,
                   _buildConfig.SerializeXml( new XElement( XmlNames.BuildAndRegisterConfiguration ) ),
                   _aspects.Select( a => a.SerializeXml( new XElement( XmlNames.Aspect, new XAttribute( XmlNames.Type, aspectTypeNameWriter( a.GetType() ) ) ) ) ) );
            return e;
        }

        /// <summary>
        /// Gets or sets the final Assembly name.
        /// When set to null (the default), <see cref="DefaultGeneratedAssemblyName"/> "CK.StObj.AutoAssembly" is returned.
        /// </summary>
        public string GeneratedAssemblyName
        {
            get => _generatedAssemblyName ?? DefaultGeneratedAssemblyName;
            set => _generatedAssemblyName = value;
        }

        /// <summary>
        /// Gets or sets whether generated source files should be generated alongside the <see cref="GeneratedAssemblyName"/>.
        /// </summary>
        public bool GenerateSourceFiles { get; set; }

        /// <summary>
        /// Gets the configuration that describes how Application Domain must be used during build and
        /// which assemlies and types must be discovered.
        /// </summary>
        public BuildAndRegisterConfiguration BuildAndRegisterConfiguration => _buildConfig;

        /// <summary>
        /// Whether the final assembly in the <see cref="AppContext.BaseDirectory"/> should be generated.
        /// Defaults to true.
        /// </summary>
        public bool GenerateAppContextAssembly { get; set; }

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

    }
}
