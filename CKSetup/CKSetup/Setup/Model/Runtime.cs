using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    class Runtime
    {
        public Runtime( TargetFramework t, CSVersion v, string n, Engine embedder = null )
        {
            TargetFramework = t;
            Version = v;
            Name = n;
            Embedder = embedder;
        }

        public TargetFramework TargetFramework { get; }

        public CSVersion Version { get; }

        public string Name { get; }

        public Engine Embedder { get; }
    }
}
