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
            GenerateSourceFiles = true;
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
            Directory = (string)e.Element( StObjEngineConfiguration.XmlNames.Directory );
            DirectoryTarget = (string)e.Element( StObjEngineConfiguration.XmlNames.DirectoryTarget );
            SkipCompilation = (bool?)e.Element( StObjEngineConfiguration.XmlNames.SkipCompilation ) ?? false;
            GenerateSourceFiles = (bool?)e.Element( StObjEngineConfiguration.XmlNames.GenerateSourceFiles ) ?? true;

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
                                    DirectoryTarget != null ? new XElement( StObjEngineConfiguration.XmlNames.DirectoryTarget, DirectoryTarget ) : null,
                                    SkipCompilation ? new XElement( StObjEngineConfiguration.XmlNames.SkipCompilation, true ) : null,
                                    GenerateSourceFiles ? null : new XElement( StObjEngineConfiguration.XmlNames.GenerateSourceFiles, false ),
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
        /// Gets or sets an optional target (output) directory where generated files (assembly and/or sources)
        /// must be copied. When null, this <see cref="Directory"/> is used.
        /// </summary>
        public string DirectoryTarget { get; set; }

        /// <summary>
        /// Gets or sets whether the compilation should be skipped for this folder (and compiled assembly shouldn't be
        /// copied to <see cref="DirectoryTarget"/>).
        /// Defaults to false.
        /// </summary>
        public bool SkipCompilation { get; set; }

        /// <summary>
        /// Gets whether generated source files should be generated and copied to <see cref="DirectoryTarget"/>.
        /// Defaults to true.
        /// </summary>
        public bool GenerateSourceFiles { get; set; }

        /// <summary>
        /// Gets a set of assembly names that must be processed for setup.
        /// Only assemblies that appear in this list will be considered.
        /// This must be a subset of the root <see cref="StObjEngineConfiguration.Assemblies"/>.
        /// </summary>
        public HashSet<string> Assemblies { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be explicitly registered 
        /// regardless of <see cref="Assemblies"/>.
        /// This must be a subset of the root <see cref="StObjEngineConfiguration.Types"/>.
        /// </summary>
        public HashSet<string> Types { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be excluded from  
        /// registration.
        /// This must be a superset of the root <see cref="StObjEngineConfiguration.ExcludedTypes"/>.
        /// </summary>
        public HashSet<string> ExcludedTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be singletons. 
        /// </summary>
        public HashSet<string> ExternalSingletonTypes { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that are known to be scoped. 
        /// </summary>
        public HashSet<string> ExternalScopedTypes { get; }


    }
}
