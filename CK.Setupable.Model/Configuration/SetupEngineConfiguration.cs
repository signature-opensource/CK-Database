using System;
using System.Collections.Generic;
using CK.Core;
using System.Xml.Linq;
using System.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Fundamental configuration objects. It holds the configuration related to StObj (<see cref="P:StObjEngineConfiguration"/>)
    /// and configuration for the <see cref="Aspects"/> that are needed.
    /// </summary>
    public class SetupEngineConfiguration : IStObjBuilderConfiguration
    {
        readonly StObjEngineConfiguration _stObjConfig;
        readonly List<ISetupEngineAspectConfiguration> _aspects;

        /// <summary>
        /// Initializes a new <see cref="SetupEngineConfiguration"/>.
        /// </summary>
        public SetupEngineConfiguration()
        {
            _stObjConfig = new StObjEngineConfiguration();
            _aspects = new List<ISetupEngineAspectConfiguration>();
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
            static public readonly XName Version = CK.Core.StObjEngineConfiguration.xVersion;
            static public readonly XName StObjEngineConfiguration = XNamespace.None + "StObjEngineConfiguration";
            static public readonly XName Aspect = XNamespace.None + "Aspect";
            static public readonly XName Type = XNamespace.None + "Type";
            static public readonly XName TraceDependencySorterInput = XNamespace.None + "TraceDependencySorterInput";
            static public readonly XName TraceDependencySorterOutput = XNamespace.None + "TraceDependencySorterOutput";
        }

        public SetupEngineConfiguration( XElement e )
        {
            int? nv = (int?)e.Attribute( XmlNames.Version );
            int v = nv.HasValue ? nv.Value : CurrentXmlVersion;

            _stObjConfig = new StObjEngineConfiguration( e.Element( XmlNames.StObjEngineConfiguration ) );
            _aspects = new List<ISetupEngineAspectConfiguration>();
            foreach( var a in e.Elements( XmlNames.Aspect ) )
            {
                string type = (string)e.AttributeRequired( XmlNames.Type );
                Type tAspect = SimpleTypeFinder.WeakResolver( type, true );
                ISetupEngineAspectConfiguration aspect = (ISetupEngineAspectConfiguration)Activator.CreateInstance( tAspect, e );
                _aspects.Add( aspect );
            }
            TraceDependencySorterInput = string.Equals( e.Element( XmlNames.TraceDependencySorterInput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            TraceDependencySorterOutput = string.Equals( e.Element( XmlNames.TraceDependencySorterOutput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The <see cref="SetupEngineConfiguration(XElement)"/> constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( new XAttribute( XmlNames.Version, CurrentXmlVersion ),
                   _stObjConfig.SerializeXml( new XElement( XmlNames.StObjEngineConfiguration ) ),
                   _aspects.Select( a => a.SerializeXml( new XElement( XmlNames.Aspect, new XAttribute( XmlNames.Type, a.GetType().AssemblyQualifiedName ) ) ) ),
                   TraceDependencySorterInput ? new XElement( XmlNames.TraceDependencySorterInput, "true" ) : null,
                   TraceDependencySorterOutput ? new XElement( XmlNames.TraceDependencySorterOutput, "true" ) : null );
            return e;
        }

        /// <summary>
        /// Gets ors sets the <see cref="SetupEngineRunningMode"/>.
        /// Defaults to <see cref="SetupEngineRunningMode.Default"/>.
        /// </summary>
        public SetupEngineRunningMode RunningMode { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of IDependentItem) must be send to the monitor before sorting.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterInput { get; set; }

        /// <summary>
        /// Gets whether the dependency graph (the set of ISortedItem) must be send to the monitor once the graph is sorted.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterOutput { get; set; }

        /// <summary>
        /// Gets the <see cref="StObjEngineConfiguration"/> object.
        /// </summary>
        public StObjEngineConfiguration StObjEngineConfiguration => _stObjConfig; 

        /// <summary>
        /// Gets the list of all configuration aspects that must participate to setup.
        /// </summary>
        public List<ISetupEngineAspectConfiguration> Aspects  => _aspects; 

        string IStObjBuilderConfiguration.BuilderAssemblyQualifiedName => "CK.Setup.SetupEngine, CK.Setupable.Engine"; 
    }
}
