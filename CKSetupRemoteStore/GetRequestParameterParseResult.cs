using CSemVer;
using Microsoft.AspNetCore.Http;
using System;

namespace CKSetupRemoteStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Either <see cref="TargetRuntime"/> or <see cref="TargetFramework"/></typeparam>
    class GetRequestParameterParseResult<T> where T : struct
    {
        /// <summary>
        /// Gets the name of the component.
        /// Cannot be null or empty.
        /// This is the first part.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the runtime or the framework.
        /// It is mandatory (otherwise parse fails).
        /// This is the second part.
        /// </summary>
        public T Target { get; }

        /// <summary>
        /// Gets the version.
        /// This is the third part and is optional.
        /// </summary>
        public SVersion Version { get; }

        /// <summary>
        /// Gets authorized monikers: "ci", "preview", or "release".
        /// This moniker and actual <see cref="Version"/> are mutually exclusive.
        /// "ci" is the same as a null Version: the very latest version is selected whatever it is.
        /// "preview" allows pre release versions as well as final releases.
        /// "release" allows only final releases.
        /// </summary>
        public string VersionMoniker { get; }

        /// <summary>
        /// Gets the error message if parse failed.
        /// </summary>
        public string ErrorMessage { get; }

        GetRequestParameterParseResult(
            string name,
            T target,
            SVersion version,
            string moniker )
        {
            Name = name;
            Target = target;
            Version = version;
            VersionMoniker = moniker;
        }

        GetRequestParameterParseResult( string error )
        {
            ErrorMessage = error;
        }

        public static GetRequestParameterParseResult<T> Parse( PathString remainder )
        {
            string[] nv = remainder.Value.Split( new[] { '/' }, StringSplitOptions.RemoveEmptyEntries );
            if( nv.Length < 2
                || nv.Length > 3 )
            {
                return new GetRequestParameterParseResult<T>( "Invalid path." );
            }
            string name = nv[0];
            if( String.IsNullOrWhiteSpace( name ) )
            {
                return new GetRequestParameterParseResult<T>( "Invalid Component name." );
            }
            T target;
            if( !Enum.TryParse( nv[1], true, out target ) )
            {
                return new GetRequestParameterParseResult<T>( $"Invalid {typeof( T ).Name}." );
            }
            SVersion version = null;
            string moniker = null;
            if( nv.Length == 3 )
            {
                string theVersion = nv[2];
                if( "ci".Equals( theVersion, StringComparison.OrdinalIgnoreCase ) )
                {
                    moniker = "ci";
                }
                else if( "preview".Equals( theVersion, StringComparison.OrdinalIgnoreCase ) )
                {
                    moniker = "preview";
                }
                else if( "release".Equals( theVersion, StringComparison.OrdinalIgnoreCase ) )
                {
                    moniker = "release";
                }
                else
                {
                    version = SVersion.TryParse( theVersion );
                    if( !version.IsValidSyntax )
                    {
                        return new GetRequestParameterParseResult<T>( $"Invalid version." );
                    }
                }
            }
            return new GetRequestParameterParseResult<T>( name, target, version, moniker );
        }
    }

}
