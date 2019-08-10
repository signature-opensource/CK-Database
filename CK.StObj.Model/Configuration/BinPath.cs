using CK.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Describes a folder to process and for which a <see cref="IStObjMap"/> should be generated,
    /// either in source code or as compiled Dynamic Linked Library.
    /// </summary>
    public class BinPath
    {
        /// <summary>
        /// Initializes a new empty <see cref="BinPath"/>.
        /// At least, the <see cref="Path"/> should be set for this BinPath to be valid.
        /// </summary>
        public BinPath()
        {
            GenerateSourceFiles = true;
            Assemblies = new HashSet<string>();
            Types = new HashSet<string>();
            ExcludedTypes = new HashSet<string>();
            ExternalSingletonTypes = new HashSet<string>();
            ExternalScopedTypes = new HashSet<string>();
        }

        /// <summary>
        /// Initializes a new <see cref="BinPath"/> from a Xml element.
        /// </summary>
        public BinPath( XElement e )
        {
            Path = (string)e.Element( StObjEngineConfiguration.XmlNames.Path );
            OutputPath = (string)e.Element( StObjEngineConfiguration.XmlNames.OutputPath );
            SkipCompilation = (bool?)e.Element( StObjEngineConfiguration.XmlNames.SkipCompilation ) ?? false;
            GenerateSourceFiles = (bool?)e.Element( StObjEngineConfiguration.XmlNames.GenerateSourceFiles ) ?? true;

            Assemblies = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.Assemblies, StObjEngineConfiguration.XmlNames.Assembly ) );
            Types = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.Types, StObjEngineConfiguration.XmlNames.Type ) );
            ExternalSingletonTypes = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.ExternalSingletonTypes, StObjEngineConfiguration.XmlNames.Type ) );
            ExternalScopedTypes = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.ExternalScopedTypes, StObjEngineConfiguration.XmlNames.Type ) );
            ExcludedTypes = new HashSet<string>( StObjEngineConfiguration.FromXml( e, StObjEngineConfiguration.XmlNames.ExcludedTypes, StObjEngineConfiguration.XmlNames.Type ) );
        }

        /// <summary>
        /// Creates a xml element from this <see cref="BinPath"/>.
        /// </summary>
        /// <returns>A new element.</returns>
        public XElement ToXml()
        {
            return new XElement( StObjEngineConfiguration.XmlNames.BinPath,
                                    new XElement( StObjEngineConfiguration.XmlNames.Path, Path ),
                                    !OutputPath.IsEmptyPath ? new XElement( StObjEngineConfiguration.XmlNames.OutputPath, OutputPath ) : null,
                                    SkipCompilation ? new XElement( StObjEngineConfiguration.XmlNames.SkipCompilation, true ) : null,
                                    GenerateSourceFiles ? null : new XElement( StObjEngineConfiguration.XmlNames.GenerateSourceFiles, false ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.Assemblies, StObjEngineConfiguration.XmlNames.Assembly, Assemblies ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.Types, StObjEngineConfiguration.XmlNames.Type, Types ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.ExternalSingletonTypes, StObjEngineConfiguration.XmlNames.Type, ExternalSingletonTypes ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.ExternalScopedTypes, StObjEngineConfiguration.XmlNames.Type, ExternalScopedTypes ),
                                    StObjEngineConfiguration.ToXml( StObjEngineConfiguration.XmlNames.ExcludedTypes, StObjEngineConfiguration.XmlNames.Type, ExcludedTypes ) );
        }

        /// <summary>
        /// Gets or sets the path of the directory to setup.
        /// This property is required (it should not be <see cref="NormalizedPath.IsEmptyPath"/>).
        /// It can be relative: it will be combined to the <see cref="StObjEngineConfiguration.BasePath"/>.
        /// </summary>
        public NormalizedPath Path { get; set; }

        /// <summary>
        /// Gets or sets an optional target (output) directory where generated files (assembly and/or sources)
        /// must be copied. When <see cref="NormalizedPath.IsEmptyPath"/>, this <see cref="Path"/> is used.
        /// </summary>
        public NormalizedPath OutputPath { get; set; }

        /// <summary>
        /// Gets or sets whether the compilation should be skipped for this folder (and compiled assembly shouldn't be
        /// copied to <see cref="OutputPath"/>).
        /// Defaults to false.
        /// </summary>
        public bool SkipCompilation { get; set; }

        /// <summary>
        /// Gets whether generated source files should be generated and copied to <see cref="OutputPath"/>.
        /// Defaults to true.
        /// </summary>
        public bool GenerateSourceFiles { get; set; }

        /// <summary>
        /// Gets a set of assembly names that must be processed for setup (only assemblies that appear in this list will be considered).
        /// Note that when using CKSetup, this list can be left empty: it is automatically filled with the "model dependent" assemblies.
        /// </summary>
        public HashSet<string> Assemblies { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be explicitly registered 
        /// regardless of <see cref="Assemblies"/>.
        /// </summary>
        public HashSet<string> Types { get; }

        /// <summary>
        /// Gets a set of assembly qualified type names that must be excluded from  
        /// registration.
        /// Note that any type appearing in <see cref="StObjEngineConfiguration.GlobalExcludedTypes"/> will also
        /// be excluded.
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
