using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using System.Reflection;

namespace CK.Resource
{
    class ResourceLoc
    {
        public readonly Assembly Assembly;
        public readonly IReadOnlyList<string> ResourcePaths;
    }

    public class SetupResourcesLoader
    {
        public static void LoadResources( Assembly a, IActivityLogger logger )
        {
            a.GetManifestResourceNames();
        }

    }
}
