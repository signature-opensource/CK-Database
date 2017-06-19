using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    class Engine
    {
        public Engine( TargetFramework t, CSVersion v, string n, IEnumerable<Runtime> embedded )
        {
            TargetFramework = t;
            Version = v;
            Name = n;
            EmbeddedRuntimes = embedded.ToArray();
        }

        public TargetFramework TargetFramework { get; }

        public CSVersion Version { get; }

        public string Name { get; }

        public IReadOnlyList<Runtime> EmbeddedRuntimes { get; }

    }
}
