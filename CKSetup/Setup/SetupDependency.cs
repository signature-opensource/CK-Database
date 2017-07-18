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
        /// Egg and chicken issue: to resolve the UseModel/RuntimeVersion the source
        /// version must be known. Checking InformationalVersion (and may be use the Zero one) is 
        /// done only in the source when we know that the source is actually a component
        /// ... because it has SetupDependencies.
        /// We use a static special SVersion (reference equality) to mark UseModel/RuntimeVersion and the 
        /// BinFileInfo ctor will mutate the UseMinVersion property.
        /// </summary>
        static SVersion UseMinVersionMarker = new SVersion( 0, 0, 0 );

        /// <summary>
        /// Called by the BnFileInfo constructor for IsModelThatUsesRuntime or IsRuntimeThatUsesEngine attributes.
        /// </summary>
        /// <param name="isModel">True for IsModelThatUsesRuntime attribute.</param>
        /// <param name="ctorArgs">Arguments of the constructor.</param>
        /// <param name="modelOrRuntime">The source model or runtime.</param>
        internal SetupDependency( bool isModel, IList<CustomAttributeArgument> ctorArgs, BinFileInfo modelOrRuntime )
        {
            IsModel = isModel;
            string attributeName = isModel ? "IsModelThatUsesRuntime" : "IsRuntimeThatUsesEngine";
            if( ctorArgs.Count != 2
                || !(ctorArgs[0].Value is string) 
                || !(ctorArgs[1].Value is string) )
            {
                throw new ArgumentException( $"{modelOrRuntime.Name.Name} has an invalid {attributeName} attribute: there must be exactly 2 string arguments." );
            }
            UseName = (string)ctorArgs[0].Value;
            if( string.IsNullOrWhiteSpace( UseName ) )
            {
                throw new ArgumentException( $"{modelOrRuntime.Name.Name} has an empty name in its {(IsModel ? "IsModelThatUsesRuntime" : "IsRuntimeThatUsesEngine")} attribute." );
            }
            if( UseName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase )
                || UseName.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
            {
                UseName = UseName.Substring( 0, UseName.Length - 4 );
            }
            string v = (string)ctorArgs[1].Value;
            if( v == "UseModelVersion" || v == "UseRuntimeVersion" )
            {
                UseMinVersion = UseMinVersionMarker;
            }
            else if( !string.IsNullOrWhiteSpace( v ) )
            {
                UseMinVersion = SVersion.TryParse( v );
                if( !UseMinVersion.IsValidSyntax )
                {
                    throw new ArgumentException( $"{modelOrRuntime.Name.Name} has an invalid version '{v}' in its {(IsModel ? "IsModelThatUsesRuntime" : "IsRuntimeThatUsesEngine")} attribute." );
                }
            }
            Source = modelOrRuntime;
        }

        public bool IsRuntime => !IsModel;

        public bool IsModel { get; }

        public BinFileInfo Source { get; }

        public string UseName { get; }

        public SVersion UseMinVersion { get; private set; }

        internal void OnSourceVersionKnown( SVersion v )
        {
            if( ReferenceEquals( UseMinVersion, UseMinVersionMarker ) )
            {
                UseMinVersion = v;
            }
        }
    }
}
