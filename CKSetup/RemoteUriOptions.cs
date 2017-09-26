using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    class RemoteUriOptions
    {
        /// <summary>
        /// The default remote store is http://cksetup.invenietis.net.
        /// </summary>
        public static readonly Uri DefaultStoreUrl = new Uri( "http://cksetup.invenietis.net" );

        public RemoteUriOptions( CommandOption url, CommandOption apiKey )
        {
            UrlOption = url;
            ApiKeyOption = apiKey;
        }

        public CommandOption UrlOption { get; }

        public CommandOption ApiKeyOption { get; }

        public Uri Url { get; private set; }

        public string ApiKey { get; private set; }

        /// <summary>
        /// Initializes this option.
        /// </summary>
        /// <param name="m">The monitor.</param>
        /// <returns>True on success, false on error.</returns>
        public bool Initialize( IActivityMonitor m )
        {
            Uri u;
            if( !UrlOption.HasValue() ) u = DefaultStoreUrl;
            else if( UrlOption.Value() == "'none'" || UrlOption.Value() == "none" )
            {
                // Nothing to do.
                return true;
            }
            else if( !Uri.TryCreate( UrlOption.Value(), UriKind.Absolute, out u ) )
            {
                m.Error( $"--remote {UrlOption.Value()} is not a valid absolute url." );
                return false;
            }
            Url = u;
            ApiKey = ApiKeyOption.Value();
            if( string.IsNullOrEmpty( ApiKey ) ) ApiKey = null;

            m.Info( $"Using remote: {Url} with" + ApiKey != null ? " an API key." : "out API key." );
            return true;
        }

    }
}
