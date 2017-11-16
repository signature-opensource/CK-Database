using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CKSetupRemoteStore
{
    public class CKSetupStoreOptions
    {
        /// <summary>
        /// Gets or sets the root store path. If not <see cref="Path.IsPathRooted"/> it is relative 
        /// to the <see cref="IHostingEnvironment.ContentRootPath"/>.
        /// Defaults to ContentRootPath/Store.
        /// </summary>
        public string RootStorePath { get; set; }

        /// <summary>
        /// Gets or sets the url prefix to download zipped components.
        /// Defaults to "/dl-zip".
        /// </summary>
        public PathString DownloadZipPrefix { get; set; }

        /// <summary>
        /// Gets or sets the url prefix to get component information.
        /// Defaults to "/component-info".
        /// </summary>
        public PathString ComponentInfoPrefix { get; set; }

        /// <summary>
        /// Gets or sets the url prefix to get the full component database information.
        /// Defaults to "/component-db".
        /// </summary>
        public PathString ComponentDbPrefix { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time of the push files
        /// validity. Defaults to 5 minutes.
        /// </summary>
        public TimeSpan PushSessionDuration { get; set; } = TimeSpan.FromMinutes( 5 );

        /// <summary>
        /// Gets or sets the list of allowed Api keys.
        /// Must not be null nor empty.
        /// </summary>
        public List<string> ApiKeys { get; set; }
    }
}
