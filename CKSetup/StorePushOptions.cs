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
    class StorePushOptions
    {
        /// <summary>
        /// The default remote store is https://cksetup.invenietis.net.
        /// </summary>
        public static readonly Uri DefaultStoreUrl = new Uri( "https://cksetup.invenietis.net" );

        public StorePushOptions( CommandOption url, CommandOption apiKey )
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
            else if( !Uri.TryCreate( UrlOption.Value(), UriKind.Absolute, out u ))
            {
                m.Error( $"--url {UrlOption.Value()} is not a valid absolute url." );
                return false;
            }
            Url = u;
            ApiKey = ApiKeyOption.Value();
            if( string.IsNullOrEmpty( ApiKey ) ) ApiKey = null;

            m.Info( $"Using remote url: {Url}" );
            return true;
        }

    }
}
