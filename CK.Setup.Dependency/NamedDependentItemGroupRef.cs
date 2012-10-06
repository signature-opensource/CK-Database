using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Defines a named reference to a Group.
    /// </summary>
    public class NamedDependentItemGroupRef : NamedDependentItemRef, IDependentItemGroupRef
    {
        /// <summary>
        /// Initializes a new <see cref="NamedDependentItemGroupRef"/> with a <see cref="FullName"/>
        /// optionaly starting with '?'.
        /// </summary>
        public NamedDependentItemGroupRef( string fullName )
            : base( fullName )
        {
        }

        /// <summary>
        /// Initializes a potentially optional new <see cref="NamedDependentItemGroupRef"/> with a <see cref="FullName"/>.
        /// </summary>
        public NamedDependentItemGroupRef( string fullName, bool optional )
            : base( fullName, optional )
        {
        }

        public static implicit operator NamedDependentItemGroupRef( string fullName )
        {
            return new NamedDependentItemGroupRef( fullName );
        }

    }
}
