using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RequiresAttribute : Attribute
    {
        readonly string _requires;

        /// <summary>
        /// Defines requirements by their names.
        /// </summary>
        /// <param name="requires">Comma separated list of requirement item names.</param>
        public RequiresAttribute( string requires )
        {
            _requires = requires;
        }

        /// <summary>
        /// Gets a comma separated list of item names.
        /// </summary>
        public string Requirements { get { return _requires; } }

    }
}
