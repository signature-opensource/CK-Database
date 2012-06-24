using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class PackageAttribute : Attribute
    {
        public PackageAttribute( string versions )
        {
            Versions = versions;
        }

        public string FullName { get; private set; }

        public string Versions { get; private set; }

        public string Requires { get; private set; }

        public string RequiredBy { get; private set; }

        public string PackageFullName { get; set; }

        public Type Package { get; set; }
    }

}
