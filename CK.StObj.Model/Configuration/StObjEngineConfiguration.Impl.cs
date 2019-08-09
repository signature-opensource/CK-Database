using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CK.Core
{
    public sealed partial class StObjEngineConfiguration 
    {
        string _generatedAssemblyName;

        /// <summary>
        /// Initializes a new empty configuration.
        /// </summary>
        public StObjEngineConfiguration()
        {
            GenerateSourceFiles = true;
            Assemblies = new HashSet<string>();
            Types = new HashSet<string>();
            ExternalSingletonTypes = new HashSet<string>();
            ExternalScopedTypes = new HashSet<string>();
            ExcludedTypes = new HashSet<string>();
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
            /// The ExternalSingletonTypes element name.
            /// </summary>
            static public readonly XName ExternalSingletonTypes = XNamespace.None + "ExternalSingletonTypes";

            /// <summary>
            /// The ExternalScopedTypes element name.
            /// </summary>
            static public readonly XName ExternalScopedTypes = XNamespace.None + "ExternalScopedTypes";

            /// <summary>
            /// The ExcludedTypes element name.
            /// </summary>
            static public readonly XName ExcludedTypes = XNamespace.None + "ExcludedTypes";

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
            /// The DirectoryTarget element name.
            /// </summary>
            static public readonly XName DirectoryTarget = XNamespace.None + "DirectoryTarget";

            /// <summary>
            /// The GenerateSourceFiles element name.
            /// </summary>
            static public readonly XName GenerateSourceFiles = XNamespace.None + "GenerateSourceFiles";

            /// <summary>
            /// The SkipCompilation element name.
            /// </summary>
            static public readonly XName SkipCompilation = XNamespace.None + "SkipCompilation";

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
            // Global options.
            TraceDependencySorterInput = (bool?)e.Element( XmlNames.TraceDependencySorterInput ) ?? false;
            TraceDependencySorterOutput = (bool?)e.Element( XmlNames.TraceDependencySorterOutput ) ?? false;
            RevertOrderingNames = (bool?)e.Element( XmlNames.RevertOrderingNames ) ?? false;
            GeneratedAssemblyName = (string)e.Element( XmlNames.GeneratedAssemblyName );
            InformationalVersion = (string)e.Element( XmlNames.InformationalVersion );
 
            // Root SetupFolder options.
            DirectoryTarget = (string)e.Element( XmlNames.DirectoryTarget )
                                // Handling previous v11 AppContextAssemblyGeneratedDirectoryTarget name.
                                ?? (string)e.Element( XNamespace.None + "AppContextAssemblyGeneratedDirectoryTarget" );
            SkipCompilation = (bool?)e.Element( XmlNames.SkipCompilation ) ?? false;
            GenerateSourceFiles = (bool?)e.Element( XmlNames.GenerateSourceFiles )?? true;

            Assemblies = new HashSet<string>( FromXml( e, XmlNames.Assemblies, XmlNames.Assembly ) );
            Types = new HashSet<string>( FromXml( e, XmlNames.Types, XmlNames.Type ) );
            ExternalSingletonTypes = new HashSet<string>( FromXml( e, XmlNames.ExternalSingletonTypes, XmlNames.Type ) );
            ExternalScopedTypes = new HashSet<string>( FromXml( e, XmlNames.ExternalScopedTypes, XmlNames.Type ) );
            ExcludedTypes = new HashSet<string>( FromXml( e, XmlNames.ExcludedTypes, XmlNames.Type ) );

            // SetupFolders.
            SetupFolders = e.Descendants( XmlNames.SetupFolder ).Select( f => new SetupFolder( f ) ).ToList();

            // Aspects.
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
        /// <returns>The <paramref name="e"/> element to fill.</returns>
        public XElement SerializeXml( XElement e )
        {
            string CleanName( Type t )
            {
                SimpleTypeFinder.WeakenAssemblyQualifiedName( t.AssemblyQualifiedName, out string weaken );
                return weaken;
            }

            e.Add( new XComment( "Please see https://gitlab.com/signature-code/CK-Database/raw/develop/CK.StObj.Model/Configuration/StObjEngineConfiguration.cs for documentation." ),
                   TraceDependencySorterInput ? new XElement( XmlNames.TraceDependencySorterInput, true ) : null,
                   TraceDependencySorterOutput ? new XElement( XmlNames.TraceDependencySorterOutput, true ) : null,
                   RevertOrderingNames ? new XElement( XmlNames.RevertOrderingNames, true ) : null,
                   GeneratedAssemblyName != DefaultGeneratedAssemblyName ? new XElement( XmlNames.GeneratedAssemblyName, GeneratedAssemblyName ) : null,
                   InformationalVersion != null ? new XElement( XmlNames.InformationalVersion, InformationalVersion ) : null,
                   DirectoryTarget != null ? new XElement( XmlNames.DirectoryTarget, DirectoryTarget ) : null,
                   SkipCompilation ? new XElement( XmlNames.SkipCompilation, true ) : null,
                   GenerateSourceFiles ? null : new XElement( XmlNames.GenerateSourceFiles, false ),
                   ToXml( XmlNames.Assemblies, XmlNames.Assembly, Assemblies ),
                   ToXml( XmlNames.Types, XmlNames.Type, Types ),
                   ToXml( XmlNames.ExcludedTypes, XmlNames.Type, ExcludedTypes ),
                   ToXml( XmlNames.ExternalSingletonTypes, XmlNames.Type, ExternalSingletonTypes ),
                   ToXml( XmlNames.ExternalScopedTypes, XmlNames.Type, ExternalScopedTypes ),
                   Aspects.Select( a => a.SerializeXml( new XElement( XmlNames.Aspect, new XAttribute( XmlNames.Type, CleanName( a.GetType() ) ) ) ) ),
                   new XComment( "Please see https://gitlab.com/signature-code/CK-Database/raw/develop/CK.StObj.Model/Configuration/SetupFolder.cs for documentation." ),
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

    }
}
