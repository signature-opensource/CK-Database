using CSemVer;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CKSetupRemoteStore
{
    class GetRequestParameterParseResult<T> where T : struct
    {
        public string Name { get; }
        public T Target { get; }
        public SVersion Version { get; }
        public string ErrorMessage { get; }

        public GetRequestParameterParseResult(
            string name,
            T target,
            SVersion version )
        {
            Name = name;
            Target = target;
            Version = version;
        }

        public GetRequestParameterParseResult( string error )
        {
            ErrorMessage = error;
        }

        public static GetRequestParameterParseResult<T> Parse( PathString remainder )
        {
            string[] nv = remainder.Value.Split( '/' );
            if( nv.Length < 3
                || nv.Length > 4 )
            {
                return new GetRequestParameterParseResult<T>( "Invalid path." );
            }
            string name = nv[1];
            if( String.IsNullOrWhiteSpace( name ) )
            {
                return new GetRequestParameterParseResult<T>( "Invalid Component name." );
            }
            T target;
            if( !Enum.TryParse( nv[2], true, out target ) )
            {
                return new GetRequestParameterParseResult<T>( $"Invalid {typeof( T ).Name}." );
            }
            SVersion version = null;
            if( nv.Length == 4 )
            {
                version = SVersion.TryParse( nv[3] );
                if( !version.IsValidSyntax )
                {
                    return new GetRequestParameterParseResult<T>( $"Invalid version." );
                }
            }
            return new GetRequestParameterParseResult<T>( name, target, version );
        }
    }

}
