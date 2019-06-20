using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Describes a folder to process.
    /// </summary>
    public class SetupFolder : ISetupFolder
    {
        /// <summary>
        /// Initializes a new empty <see cref="SetupFolder"/>.
        /// </summary>
        public SetupFolder()
        {
            Assemblies = new HashSet<string>();
            Types = new HashSet<string>();
            ExcludedTypes = new HashSet<string>();
            ExternalSingletonTypes = new HashSet<string>();
            ExternalScopedTypes = new HashSet<string>();
        }

        /// <summary>
        /// Initializes a new <see cref="SetupFolder"/> from a Xml element.
        /// </summary>
        public SetupFolder( XElement e )
        {
            Directory = e.Element( StObjEngineConfiguration.XmlNames.Directory )?.Value;
            Assemblies = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.Assemblies, StObjEngineConfiguration.XmlNames.Assembly ) );
            Types = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.Types, StObjEngineConfiguration.XmlNames.Type ) );
            ExternalSingletonTypes = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.ExternalSingletonTypes, StObjEngineConfiguration.XmlNames.Type ) );
            ExternalScopedTypes = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.ExternalScopedTypes, StObjEngineConfiguration.XmlNames.Type ) );
            ExcludedTypes = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.ExcludedTypes, StObjEngineConfiguration.XmlNames.Type ) );
        }

        /// <summary>
        /// Creates a xml element from this <see cref="SetupFolder"/>.
        /// </summary>
        /// <returns>A new element.</returns>
        public XElement ToXml()
        {
            return new XElement( StObjEngineConfiguration.XmlNames.SetupFolder,
                                    new XElement( StObjEngineConfiguration.XmlNames.Directory, Directory ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.Assemblies, StObjEngineConfiguration.XmlNames.Assembly, Assemblies ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.Types, StObjEngineConfiguration.XmlNames.Type, Types ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.ExternalSingletonTypes, StObjEngineConfiguration.XmlNames.Type, ExternalSingletonTypes ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.ExternalScopedTypes, StObjEngineConfiguration.XmlNames.Type, ExternalScopedTypes ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.ExcludedTypes, StObjEngineConfiguration.XmlNames.Type, ExcludedTypes ) );
        }

        /// <summary>
        /// Gets or sets the path of the directory into which a subset of the global setup
        /// must be generated.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Gets a set of assembly names that must be processed for setup.
        /// Only assemblies that appear in this list will be considered.
        /// </summary>
        public HashSet<string> Assemblies { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be explicitly registered 
        /// regardless of <see cref="Assemblies"/>.
        /// </summary>
        public HashSet<string> Types { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be singletons. 
        /// </summary>
        public HashSet<string> ExternalSingletonTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be scoped. 
        /// </summary>
        public HashSet<string> ExternalScopedTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be excluded from  
        /// registration.
        /// </summary>
        public HashSet<string> ExcludedTypes { get; }

    }
}
