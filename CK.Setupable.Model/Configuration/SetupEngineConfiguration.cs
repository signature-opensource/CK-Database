using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;
using System.Xml.Linq;

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

        static readonly XName xStObjEngineConfiguration = XNamespace.None + "StObjEngineConfiguration";
        static readonly XName xAspect = XNamespace.None + "Aspect";
        static readonly XName xType = XNamespace.None + "Type";

        public SetupEngineConfiguration( XElement e )
        {
            _stObjConfig = new StObjEngineConfiguration( e.Element( xStObjEngineConfiguration ) );
            _aspects = new List<ISetupEngineAspectConfiguration>();
            foreach( var a in e.Elements( xAspect ) )
            {
                string type = (string)e.AttributeRequired( xType );
                Type tAspect = SimpleTypeFinder.WeakResolver( type, true );
                ISetupEngineAspectConfiguration aspect = (ISetupEngineAspectConfiguration)Activator.CreateInstance( tAspect, e );
                _aspects.Add( aspect );
            }
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
