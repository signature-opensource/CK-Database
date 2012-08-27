using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class ContainerAttribute : Attribute
    {
        public ContainerAttribute( string fullName )
        {
            FullName = fullName;
        }

        /// <summary>
        /// Gets or sets the container full name. 
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the container of this container.
        /// </summary>
        public Type Container { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of full names dependencies.
        /// </summary>
        public string Requires { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of full names that depends on this container.
        /// </summary>
        public string RequiredBy { get; set; }

    }
}
