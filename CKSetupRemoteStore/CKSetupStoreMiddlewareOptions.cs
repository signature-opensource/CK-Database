using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CKSetupRemoteStore
{
    public class CKSetupStoreMiddlewareOptions
    {
        /// <summary>
        /// Gets or sets the root store path.
        /// </summary>
        public string RootStorePath { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time of the push files
        /// validity. Defaults to 3 seconds.
        /// </summary>
        public TimeSpan PushSessionDuration { get; set; } = TimeSpan.FromSeconds( 3 );
    }
}
