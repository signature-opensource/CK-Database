using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CKSetupRemoteStore
{
    static class InternalExtensions
    {
        static public void SetNoCacheAndDefaultStatus( this HttpResponse @this, int defaultStatusCode )
        {
            @this.Headers[HeaderNames.CacheControl] = "no-cache";
            @this.Headers[HeaderNames.Pragma] = "no-cache";
            @this.Headers[HeaderNames.Expires] = "-1";
            @this.StatusCode = defaultStatusCode;
        }
    }
}
