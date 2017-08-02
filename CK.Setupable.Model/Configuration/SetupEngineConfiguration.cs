using System;
using System.Collections.Generic;
using CK.Core;
using System.Xml.Linq;
using System.Linq;
using System.Diagnostics;

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
        /// Initializes a new empty <see cref="SetupEngineConfiguration"/>.
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
            /// <summary>
            /// The version attribute name.
            /// </summary>
            static public readonly XName Version = CK.Core.StObjEngineConfiguration.xVersion;

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
            /// The TraceDependencySorterInput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterInput = XNamespace.None + "TraceDependencySorterInput";

            /// <summary>
            /// The TraceDependencySorterOutput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterOutput = XNamespace.None + "TraceDependencySorterOutput";
        }

        /// <summary>
        /// Reads a <see cref="XElement"/> content (typically created by <see cref="SerializeXml(XElement, Func{Type, string})"/>).
        /// </summary>
        /// <param name="e">The element to read.</param>
        /// <param name="typeResolver">
        /// Resolver for types.
        /// Defaults to a function that calls <see cref="SimpleTypeFinder.WeakResolver"/>.
        /// </param>
        public SetupEngineConfiguration( XElement e, Func<string,Type> typeResolver = null )
        {
            if( typeResolver == null )
            {
                typeResolver = aqn => SimpleTypeFinder.WeakResolver( aqn, true );
            }
            int? nv = (int?)e.Attribute( XmlNames.Version );
            int v = nv.HasValue ? nv.Value : CurrentXmlVersion;

            _stObjConfig = new StObjEngineConfiguration( e.Element( XmlNames.StObjEngineConfiguration ) );
            _aspects = new List<ISetupEngineAspectConfiguration>();
            foreach( var a in e.Elements( XmlNames.Aspect ) )
            {
                string type = (string)a.AttributeRequired( XmlNames.Type );
                Type tAspect = SimpleTypeFinder.WeakResolver( type, true );
                ISetupEngineAspectConfiguration aspect = (ISetupEngineAspectConfiguration)Activator.CreateInstance( tAspect, a );
                _aspects.Add( aspect );
            }
            TraceDependencySorterInput = string.Equals( e.Element( XmlNames.TraceDependencySorterInput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            TraceDependencySorterOutput = string.Equals( e.Element( XmlNames.TraceDependencySorterOutput )?.Value, "true", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <param name="typeNameWriter">
        /// Writer for aspects type names. 
        /// Defaults to a function that returns <see cref="Type.AssemblyQualifiedName"/>.
        /// </param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e, Func<Type,string> typeNameWriter = null )
        {
            if( typeNameWriter == null )
            {
                typeNameWriter = t => t.AssemblyQualifiedName;
            }
            e.Add( new XAttribute( XmlNames.Version, CurrentXmlVersion ),
                   _stObjConfig.SerializeXml( new XElement( XmlNames.StObjEngineConfiguration ) ),
                   _aspects.Select( a => a.SerializeXml( new XElement( XmlNames.Aspect, new XAttribute( XmlNames.Type, typeNameWriter( a.GetType() ) ) ) ) ),
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
