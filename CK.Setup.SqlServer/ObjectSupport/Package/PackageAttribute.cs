using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class PackageAttribute : Attribute
    {
        public PackageAttribute( string fullName, string versions )
        {
            FullName = fullName;
            Versions = versions;
        }

        /// <summary>
        /// Gets the version list.
        /// </summary>
        public string Versions { get; private set; }

        /// <summary>
        /// Gets or sets the package full name. 
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the setup container.
        /// </summary>
        public Type Container { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of full names dependencies.
        /// </summary>
        public string Requires { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of full names that must depend on this package.
        /// </summary>
        public string RequiredBy { get; set; }

    }
}
