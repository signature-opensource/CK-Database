using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates configuration of the StObjEngine.
    /// </summary>
    public sealed class StObjEngineConfiguration : ISetupFolder
    {
        /// <summary>
        /// Default assembly name.
        /// </summary>
        public const string DefaultGeneratedAssemblyName = "CK.StObj.AutoAssembly";

        string _generatedAssemblyName;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public StObjEngineConfiguration()
        {
            Assemblies = new HashSet<string>();
            Types = new HashSet<string>();
            Aspects = new List<IStObjEngineAspectConfiguration>();
            SetupFolders = new List<SetupFolder>();
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
            /// The Aspect element name.
            /// </summary>
            static public readonly XName Aspect = XNamespace.None + "Aspect";

            /// <summary>
            /// The Assemblies element name.
            /// </summary>
            static public readonly XName Assemblies = XNamespace.None + "Assemblies";

            /// <summary>
            /// The Assembly element name.
            /// </summary>
            static public readonly XName Assembly = XNamespace.None + "Assembly";

            /// <summary>
            /// The Types element name.
            /// </summary>
            static public readonly XName Types = XNamespace.None + "Types";

            /// <summary>
            /// The Type element name.
            /// </summary>
            static public readonly XName Type = XNamespace.None + "Type";

            /// <summary>
            /// The SetupFolder element name.
            /// </summary>
            static public readonly XName SetupFolder = XNamespace.None + "SetupFolder";

            /// <summary>
            /// The Directory element name.
            /// </summary>
            static public readonly XName Directory = XNamespace.None + "Directory";

            /// <summary>
            /// The RevertOrderingNames element name.
            /// </summary>
            static public readonly XName RevertOrderingNames = XNamespace.None + "RevertOrderingNames";

            /// <summary>
            /// The GenerateAppContextAssembly element name.
            /// </summary>
            static public readonly XName ForceAppContextAssemblyGeneration = XNamespace.None + "GenerateAppContextAssembly";

            /// <summary>
            /// The GenerateSourceFiles element name.
            /// </summary>
            static public readonly XName GenerateSourceFiles = XNamespace.None + "GenerateSourceFiles";

            /// <summary>
            /// The TraceDependencySorterInput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterInput = XNamespace.None + "TraceDependencySorterInput";

            /// <summary>
            /// The TraceDependencySorterOutput element name.
            /// </summary>
            static public readonly XName TraceDependencySorterOutput = XNamespace.None + "TraceDependencySorterOutput";

            /// <summary>
            /// The GeneratedAssemblyName element name.
            /// </summary>
            static public readonly XName GeneratedAssemblyName = XNamespace.None + "GeneratedAssemblyName";

            /// <summary>
            /// The InformationalVersion element name.
            /// </summary>
            static public readonly XName InformationalVersion = XNamespace.None + "InformationalVersion";

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
            ForceAppContextAssemblyGeneration = string.Equals( e.Element( XmlNames.ForceAppContextAssemblyGeneration )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            GeneratedAssemblyName = e.Element( XmlNames.GeneratedAssemblyName )?.Value;
            InformationalVersion = e.Element( XmlNames.InformationalVersion )?.Value;
            GenerateSourceFiles = string.Equals( e.Element( XmlNames.RevertOrderingNames )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            Assemblies = new HashSet<string>( FromXml( e, XmlNames.Assemblies, XmlNames.Assembly ) );
            Types = new HashSet<string>( FromXml( e, XmlNames.Types, XmlNames.Type ) );
            SetupFolders = e.Descendants( XmlNames.SetupFolder ).Select( f => new SetupFolder( f ) ).ToList();
            Aspects = new List<IStObjEngineAspectConfiguration>();
            foreach( var a in e.Elements( XmlNames.Aspect ) )
            {
                string type = (string)a.AttributeRequired( XmlNames.Type );
                Type tAspect = SimpleTypeFinder.WeakResolver( type, true );
                IStObjEngineAspectConfiguration aspect = (IStObjEngineAspectConfiguration)Activator.CreateInstance( tAspect, a );
                Aspects.Add( aspect );
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
                   ForceAppContextAssemblyGeneration ? new XElement( XmlNames.ForceAppContextAssemblyGeneration, "true" ) : null,
                   GenerateSourceFiles ? new XElement( XmlNames.GenerateSourceFiles, "true" ) : null,
                   GeneratedAssemblyName != DefaultGeneratedAssemblyName
                        ? new XElement( XmlNames.GeneratedAssemblyName, GeneratedAssemblyName )
                        : null,
                   InformationalVersion != null
                        ? new XElement( XmlNames.InformationalVersion, InformationalVersion )
                        : null,
                   ToXml( XmlNames.Assemblies, XmlNames.Assembly, Assemblies ),
                   ToXml( XmlNames.Types, XmlNames.Type, Types ),
                   Aspects.Select( a => a.SerializeXml( new XElement( XmlNames.Aspect, new XAttribute( XmlNames.Type, aspectTypeNameWriter( a.GetType() ) ) ) ) ),
                   SetupFolders.Select( f => f.ToXml() ) );
            return e;
        }

        static internal XElement ToXml( XName names, XName name, IEnumerable<string> strings )
        {
            return new XElement( names, strings.Select( n => new XElement( name, n ) ) );
        }

        static internal IEnumerable<string> FromXml( XElement e, XName names, XName name )
        {
            return e.Elements( names ).Elements( name ).Select( c => c.Value );
        }

        /// <summary>
        /// Gets or sets the final Assembly name.
        /// When set to null (the default), <see cref="DefaultGeneratedAssemblyName"/> "CK.StObj.AutoAssembly" is returned.
        /// </summary>
        public string GeneratedAssemblyName
        {
            get => String.IsNullOrWhiteSpace(_generatedAssemblyName) ? DefaultGeneratedAssemblyName : _generatedAssemblyName;
            set => _generatedAssemblyName = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Diagnostics.FileVersionInfo.ProductVersion"/> of
        /// the <see cref="GeneratedAssemblyName"/> assembly or assemblies.
        /// Defaults to null (no <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/> should be generated).
        /// </summary>
        public string InformationalVersion { get; set; }

        /// <summary>
        /// Gets or sets whether generated source files should be generated alongside the <see cref="GeneratedAssemblyName"/>.
        /// Defaults to false.
        /// </summary>
        public bool GenerateSourceFiles { get; set; }

        /// <summary>
        /// Gets the <see cref="AppContext.BaseDirectory"/> since this were the whole setup process
        /// must be runned.
        /// </summary>
        public string Directory => AppContext.BaseDirectory;

        /// <summary>
        /// Gets a set of assembly names that must be processed in <see cref="AppContext.BaseDirectory"/> for setup.
        /// Only assemblies that appear in this list will be considered.
        /// </summary>
        public HashSet<string> Assemblies { get; }

        /// <summary>
        /// List of assembly qualified type names that must be explicitely registered 
        /// in <see cref="AppContext.BaseDirectory"/> regardless of <see cref="Assemblies"/>.
        /// All other types in the assemblies that contain these explicit classes are ignored.
        /// </summary>
        public HashSet<string> Types { get; }

        /// <summary>
        /// Gets a list of optional <see cref="SetupFolder"/>.
        /// Their assemblies and explicit classes must be subsets of <see cref="Assemblies"/> and <see cref="Types"/>
        /// for this configuration to be valid.
        /// </summary>
        public IList<SetupFolder> SetupFolders { get; }

        /// <summary>
        /// Whether the final assembly in the <see cref="AppContext.BaseDirectory"/> should always be generated.
        /// Defaults to false.
        /// The only case where this default configuration (false) is ignored is actually honored (by
        /// skipping the compilation step) is when there are multiple <see cref="SetupFolder"/>
        /// and none of them contains the whole (unified) set of components.
        /// </summary>
        public bool ForceAppContextAssemblyGeneration { get; set; }

        /// <summary>
        /// Gets the list of all configuration aspects that must participate to setup.
        /// </summary>
        public List<IStObjEngineAspectConfiguration> Aspects { get; }

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
