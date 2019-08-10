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
            Aspects = new List<IStObjEngineAspectConfiguration>();
            BinPaths = new List<BinPath>();
            GlobalExcludedTypes = new HashSet<string>();
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
            /// The ExcludedTypes element name.
            /// </summary>
            static public readonly XName GlobalExcludedTypes = XNamespace.None + "GlobalExcludedTypes";

            /// <summary>
            /// The Type element name.
            /// </summary>
            static public readonly XName Type = XNamespace.None + "Type";

            /// <summary>
            /// The BasePath element name.
            /// </summary>
            static public readonly XName BasePath = XNamespace.None + "BasePath";

            /// <summary>
            /// The BinPath element name.
            /// </summary>
            static public readonly XName BinPath = XNamespace.None + "BinPath";

            /// <summary>
            /// The BinPaths element name.
            /// </summary>
            static public readonly XName BinPaths = XNamespace.None + "BinPaths";

            /// <summary>
            /// The Path element name.
            /// </summary>
            static public readonly XName Path = XNamespace.None + "Path";

            /// <summary>
            /// The RevertOrderingNames element name.
            /// </summary>
            static public readonly XName RevertOrderingNames = XNamespace.None + "RevertOrderingNames";

            /// <summary>
            /// The OutputPath element name.
            /// </summary>
            static public readonly XName OutputPath = XNamespace.None + "OutputPath";

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
            BasePath = (string)e.Element( XmlNames.BasePath );
            GeneratedAssemblyName = (string)e.Element( XmlNames.GeneratedAssemblyName );
            TraceDependencySorterInput = (bool?)e.Element( XmlNames.TraceDependencySorterInput ) ?? false;
            TraceDependencySorterOutput = (bool?)e.Element( XmlNames.TraceDependencySorterOutput ) ?? false;
            RevertOrderingNames = (bool?)e.Element( XmlNames.RevertOrderingNames ) ?? false;
            InformationalVersion = (string)e.Element( XmlNames.InformationalVersion );
 
            GlobalExcludedTypes = new HashSet<string>( FromXml( e, XmlNames.GlobalExcludedTypes, XmlNames.Type ) );

            // BinPaths.
            BinPaths = e.Elements( XmlNames.BinPaths ).Elements( XmlNames.BinPath ).Select( f => new BinPath( f ) ).ToList();

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
                   !BasePath.IsEmptyPath ? new XElement( XmlNames.BasePath, BasePath ) : null,
                   GeneratedAssemblyName != DefaultGeneratedAssemblyName ? new XElement( XmlNames.GeneratedAssemblyName, GeneratedAssemblyName ) : null,
                   TraceDependencySorterInput ? new XElement( XmlNames.TraceDependencySorterInput, true ) : null,
                   TraceDependencySorterOutput ? new XElement( XmlNames.TraceDependencySorterOutput, true ) : null,
                   RevertOrderingNames ? new XElement( XmlNames.RevertOrderingNames, true ) : null,
                   InformationalVersion != null ? new XElement( XmlNames.InformationalVersion, InformationalVersion ) : null,
                   ToXml( XmlNames.GlobalExcludedTypes, XmlNames.Type, GlobalExcludedTypes ),
                   Aspects.Select( a => a.SerializeXml( new XElement( XmlNames.Aspect, new XAttribute( XmlNames.Type, CleanName( a.GetType() ) ) ) ) ),
                   new XComment( "BinPaths: please see https://gitlab.com/signature-code/CK-Database/raw/develop/CK.StObj.Model/Configuration/BinPath.cs for documentation." ),
                   new XElement( XmlNames.BinPaths, BinPaths.Select( f => f.ToXml() ) ) );
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
