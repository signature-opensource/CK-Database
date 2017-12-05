using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CKSetup
{
    /// <summary>
    /// Describes basic configuration object.
    /// </summary>
    public class SetupConfiguration
    {
        /// <summary>
        /// Name of the root CKSetup element. This must be the first element in the xml document,
        /// the second one being the engine configuration that becomes the root of runner configuration file.
        /// A new CKSetup element is injected at the end of this root of runner configuration file that
        /// contains the <see cref="xEngineAssemblyQualifiedName"/> element and <see cref="xBinPaths"/>.
        /// </summary>
        public static readonly XName xCKSetup = XNamespace.None + "CKSetup";

        /// <summary>
        /// Name of the EngineAssemblyQualifiedName.
        /// This element also appears in the runner configuration file's CKSetup element.
        /// </summary>
        public static readonly XName xEngineAssemblyQualifiedName = XNamespace.None + "EngineAssemblyQualifiedName";

        /// <summary>
        /// Name of the BinPaths element in <see cref="xCKSetup"/>.
        /// This element also appears in the runner configuration file's CKSetup element but its <see cref="xBinPath"/>
        /// children are different..
        /// </summary>
        public static readonly XName xBinPaths = XNamespace.None + "BinPaths";

        /// <summary>
        /// Name of a path to consider as a source for setup.
        /// This element also appears in the runner configuration file's CKSetup/BinPaths element
        /// where they have a BinPath attribute with the normalized full path of the folder
        /// and <see cref="xModel"/>, <see cref="xDependency"/> and <see cref="xModelDependent"/>
        /// children elements that contain an assembly name.
        /// </summary>
        public static readonly XName xBinPath = XNamespace.None + "BinPath";

        /// <summary>
        /// This element appears in the runner configuration file's CKSetup/BinPaths/BinPath elements.
        /// Its value is an assembly name that is a Model.
        /// </summary>
        public static readonly XName xModel = XNamespace.None + "Model";

        /// <summary>
        /// This element appears in the runner configuration file's CKSetup/BinPaths/BinPath elements.
        /// Its value is an assembly name that is a Dependency.
        /// </summary>
        public static readonly XName xDependency = XNamespace.None + "Dependency";

        /// <summary>
        /// This element appears in the runner configuration file's CKSetup/BinPaths/BinPath elements.
        /// Its value is an assembly name that depends on a Model.
        /// </summary>
        public static readonly XName xModelDependent = XNamespace.None + "ModelDependent";

        static readonly XName xWorkingDirectory = XNamespace.None + "WorkingDirectory";
        static readonly XName xDependencies = XNamespace.None + "Dependencies";
        static readonly XName xConfigurationDefaultName = XNamespace.None + "Configuration";

        XElement _configuration;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public SetupConfiguration()
        {
            BinPaths = new List<string>();
            Dependencies = new List<SetupDependency>();
            _configuration = new XElement( xConfigurationDefaultName );
        }

        /// <summary>
        /// Initializes a new configuration from a xml document.
        /// The document must have exactly one CKSetup with valid elements and another element (the <see cref="Configuration"/>)
        /// otherwise an exception is thrown.
        /// </summary>
        /// <param name="d">A valid xml document.</param>
        public SetupConfiguration( XDocument d )
        {
            var elements = d.Root.Elements().ToList();
            if( elements.Count != 2 ) throw new ArgumentException( $"Xml document must have 2 and only 2 elements (found {elements.Count})." );
            var ckSetup = elements[0];
            if( ckSetup.Name != xCKSetup ) throw new ArgumentException( $"First element name must be '{xCKSetup}', not '{ckSetup.Name}')." );

            WorkingDirectory = ckSetup.Element( xWorkingDirectory )?.Value;

            BinPaths = ckSetup.Elements( xBinPaths )
                                .Elements( xBinPaths )
                                .Select( e => e.Value )
                                .ToList();

            Dependencies = ckSetup.Elements( xDependencies )
                                    .Elements( SetupDependency.xDependency )
                                    .Select( e => new SetupDependency( e ) )
                                    .ToList(); ;

            var engine = ckSetup.Elements( xEngineAssemblyQualifiedName );
            if( engine.Count() != 1 ) throw new ArgumentException( $"{xCKSetup} element must contain one and only one '{xEngineAssemblyQualifiedName}' element." );
            EngineAssemblyQualifiedName = engine.Single().Value;
            _configuration = elements[1];
        }

        /// <summary>
        /// Creates a xml document from this configuration.
        /// </summary>
        /// <returns></returns>
        public XDocument ToXml()
        {
            return new XDocument( new XElement( xCKSetup,
                                        !string.IsNullOrWhiteSpace( WorkingDirectory)
                                            ? new XElement(xWorkingDirectory, WorkingDirectory )
                                            : null,
                                        new XElement( xBinPaths, BinPaths.Select( p => new XElement( xBinPath, p ) ) ),
                                        new XElement( xDependencies, Dependencies.Select( p => p.ToXml() ) ),
                                        new XElement( xEngineAssemblyQualifiedName, EngineAssemblyQualifiedName ) ),
                                 Configuration );
        }

        /// <summary>
        /// Gets or sets the working directory.
        /// When null or empty, a temporary folder is used.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets a list of setup dependencies.
        /// </summary>
        public List<SetupDependency> Dependencies { get; }

        /// <summary>
        /// Gets a list of binary paths to setup.
        /// It must not be empty, all paths must be valid.
        /// When the same assembly belongs to different folders, it must
        /// be exactly the same file.
        /// </summary>
        public List<string> BinPaths { get; } 

        /// <summary>
        /// Assembly qualified name of the engine type.
        /// It can be any object with a public constructor with two parameters, 
        /// a <see cref="IActivityMonitor"/> and a <see cref="XElement"/> and
        /// a public <c>bool Run()</c> method.
        /// <para>
        /// Example: "CK.Setup.StObjEngine, CK.StObj.Engine"
        /// </para>
        /// </summary>
        public string EngineAssemblyQualifiedName { get; set; }

        /// <summary>
        /// Gets or sets the configuration that will be provided to the engine.
        /// It must not be null and can have any name (defaults to "Configuration").
        /// </summary>
        public XElement Configuration
        {
            get => _configuration;
            set => _configuration = value ?? throw new ArgumentNullException();
        }
    }
}
