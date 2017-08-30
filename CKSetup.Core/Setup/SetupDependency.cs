using CSemVer;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{

    public class SetupDependency 
    {
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
            Source = source;
        }

        public BinFileAssemblyInfo Source { get; }

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
