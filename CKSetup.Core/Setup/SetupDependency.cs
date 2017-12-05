using CK.Core;
using CSemVer;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{

    public class SetupDependency 
    {
        /// <summary>
        /// Default name of a manual dependency source.
        /// </summary>
        public const string ManualSourceName = "(Manual)";

        /// <summary>
        /// Initializes a manual, explicit, setup dependency.
        /// </summary>
        /// <param name="useName">The assembly name of the dependency. Must not end with .exe or .dll.</param>
        /// <param name="sourceName">The source name should be the name of the assembly that requires this dependency.</param>
        /// <param name="useMinVersion">The minimal version. Null to let the version free.</param>
        public SetupDependency( string useName, SVersion useMinVersion = null, string sourceName = ManualSourceName )
        {
            if( String.IsNullOrWhiteSpace( sourceName ) ) throw new ArgumentNullException( nameof( sourceName ) );
            if( String.IsNullOrWhiteSpace( useName ) ) throw new ArgumentNullException( nameof( useName ) );
            SourceName = sourceName;
            UseName = useName;
            UseMinVersion = useMinVersion;
        }

        public static readonly XName xDependency = XNamespace.None + "Dependency";
        static readonly XName xSource = XNamespace.None + "Source";
        static readonly XName xName = XNamespace.None + "Name";
        static readonly XName xMinVersion = XNamespace.None + "MinVersion";

        /// <summary>
        /// Initializes a new dependency from a <see cref="XElement"/>.
        /// </summary>
        /// <param name="e">The xml element.</param>
        public SetupDependency( XElement e )
        {
            SourceName = (string)e.Attribute( xSource ) ?? ManualSourceName;
            UseName = (string)e.Attribute( xName );
            if( String.IsNullOrWhiteSpace( UseName ) ) throw new ArgumentNullException( "SetupDependency must have a non empty Name attribute.", nameof(e) );
            var v = (string)e.Attribute( xMinVersion );
            if( !String.IsNullOrWhiteSpace( v ) )
            {
                UseMinVersion = SVersion.Parse( v );
            }
        }

        /// <summary>
        /// Creates a Dependency xml element.
        /// </summary>
        /// <returns>The xml element.</returns>
        public XElement ToXml()
        {
            return new XElement( xDependency,
                                 SourceName != ManualSourceName ? new XAttribute( xSource, SourceName ) : null,
                                 new XAttribute( xName, UseName ),
                                 UseMinVersion != null ? new XAttribute( xMinVersion, UseMinVersion.Text ) : null
                               );
        }

        /// <summary>
        /// We use a static special SVersion (reference equality) to mark UseThisVersion and the 
        /// BinFileAssemblyInfo ctor will mutate the UseMinVersion property.
        /// </summary>
        static SVersion UseThisVersionMarker = new SVersion( 0, 0, 0 );

        /// <summary>
        /// Called by the BinFileAssemblyInfo constructor for RequiredSetupDependency attributes.
        /// </summary>
        /// <param name="ctorArgs">Arguments of the constructor.</param>
        /// <param name="source">The source model or setup dependency.</param>
        internal SetupDependency( IList<CustomAttributeArgument> ctorArgs, BinFileAssemblyInfo source )
        {
            if( ctorArgs.Count != 2
                || !(ctorArgs[0].Value is string)
                || !(ctorArgs[1].Value == null || ctorArgs[1].Value is string) )
            {
                throw new ArgumentException( $"{source.Name.Name} has an invalid RequiredSetupDependency attribute: there must be a first non null string and a second nullable string arguments." );
            }
            UseName = (string)ctorArgs[0].Value;
            if( string.IsNullOrWhiteSpace( UseName ) )
            {
                throw new ArgumentException( $"{source.Name.Name} has an empty name in its RequiredSetupDependency attribute." );
            }
            if( UseName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase )
                || UseName.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
            {
                UseName = UseName.Substring( 0, UseName.Length - 4 );
            }
            string v = (string)ctorArgs[1].Value;
            if( v == "UseThisVersion" )
            {
                UseMinVersion = UseThisVersionMarker;
            }
            else if( !string.IsNullOrWhiteSpace( v ) )
            {
                UseMinVersion = SVersion.TryParse( v );
                if( !UseMinVersion.IsValidSyntax )
                {
                    throw new ArgumentException( $"{source.Name.Name} has an invalid version '{v}' in its RequiredSetupDependency attribute." );
                }
            }
            SourceName = source.Name.Name;
        }

        /// <summary>
        /// Gets the source name of this SetupDependency.
        /// It is the short name of the assembly that declares this dependency
        /// or the "(Manual)" marker.
        /// </summary>
        public string SourceName { get; }

        public string UseName { get; }

        public SVersion UseMinVersion { get; private set; }

        internal void OnSourceVersionKnown( SVersion v )
        {
            if( ReferenceEquals( UseMinVersion, UseThisVersionMarker ) )
            {
                UseMinVersion = v;
            }
        }
    }
}
