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
        /// Called by the BnFileInfo constructor for IsModelThatUsesRuntime or IsRuntimeThatUsesEngine attributes.
        /// </summary>
        /// <param name="isModel">True for IsModelThatUsesRuntime attribute.</param>
        /// <param name="ctorArgs">Arguments of the constructor.</param>
        /// <param name="modelOrRuntime">The source model or runtime.</param>
        internal SetupDependency( bool isModel, IList<CustomAttributeArgument> ctorArgs, BinFileInfo modelOrRuntime )
        {
            IsModel = isModel;
            if( ctorArgs.Count > 0 )
            {
                UseName = ctorArgs[0].Value as string;
                if( string.IsNullOrWhiteSpace( UseName ) )
                {
                    throw new ArgumentException( $"{modelOrRuntime.Name.Name} has an empty name in its {(IsModel ? "IsModelThatUsesRuntime" : "IsRuntimeThatUsesEngine")} attribute." );
                }
                if( UseName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase )
                    || UseName.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
                {
                    UseName = UseName.Substring( 0, UseName.Length - 4 );
                }
            }
            if( ctorArgs.Count > 1 )
            {
                string v = (string)ctorArgs[1].Value;
                if( v == "UseModelVersion" || v == "UseRuntimeVersion" )
                {
                    // If this is null, this means that the Source
                    // is not a valid Component and this will be detected from the 
                    // BinFileInfo constructor.
                    UseMinVersion = Source?.InfoVersion?.NuGetVersion;
                }
                else if( !string.IsNullOrWhiteSpace( v ) )
                {
                    UseMinVersion = SVersion.TryParse( v );
                    if( !UseMinVersion.IsValidSyntax )
                    {
                        throw new ArgumentException( $"{modelOrRuntime.Name.Name} has an invalid version '{v}' in its {(IsModel ? "IsModelThatUsesRuntime" : "IsRuntimeThatUsesEngine")} attribute." );
                    }
                }
            }
            Source = modelOrRuntime;
        }

        public bool IsRuntime => !IsModel;

        public bool IsModel { get; }

        public BinFileInfo Source { get; }

        public string UseName { get; }

        public SVersion UseMinVersion { get; }
    }
}
