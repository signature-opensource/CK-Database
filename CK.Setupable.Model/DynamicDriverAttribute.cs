using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{

    /// <summary>
    /// Declares a DynamicDriver associated to the object.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class DynamicDriverAttribute : AmbientContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="DynamicDriverAttribute"/> with (potentially) multiple type names
        /// that must be in the associated ".Runtime" assembly.
        /// </summary>
        /// <param name="commaSeparatedTypeNames">Name or multiple comma separated names.</param>
        public DynamicDriverAttribute( string commaSeparatedTypeNames )
            : base( "CK.Setup.DynamicDriverAttributeImpl, CK.Setupable.Runtime" )
        {
            CommaSeparatedTypeNames = commaSeparatedTypeNames;
        }

        /// <summary>
        /// Gets a driver type name or multiple comma separated names.
        /// </summary>
        public string CommaSeparatedTypeNames { get; private set; }


    }
}
