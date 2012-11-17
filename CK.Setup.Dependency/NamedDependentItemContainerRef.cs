using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{

    /// <summary>
    /// Implements a named reference to a container.
    /// </summary>
    public class NamedDependentItemContainerRef : NamedDependentItemGroupRef, IDependentItemContainerRef
    {
        /// <summary>
        /// Initializes a new <see cref="NamedDependentItemContainerRef"/> with a <see cref="FullName"/>
        /// optionaly starting with '?'.
        /// </summary>
        public NamedDependentItemContainerRef( string fullName )
            : base( fullName )
        {
        }

        /// <summary>
        /// Initializes a potentially optional new <see cref="NamedDependentItemContainerRef"/> with a <see cref="FullName"/>.
        /// </summary>
        public NamedDependentItemContainerRef( string fullName, bool optional )
            : base( fullName, optional )
        {
        }

        /// <summary>
        /// Makes sure that this <see cref="FullName"/> has a [context] prefix (and returns this instance) or
        /// creates a new <see cref="NamedDependentItemContainerRef"/> with the given prefix.
        /// </summary>
        /// <param name="defaultContextName">Context name to inject if no context prefix exists.</param>
        /// <returns>This instance or a new prefixed one.</returns>
        public new NamedDependentItemContainerRef EnsureContextPrefix( string defaultContextName )
        {
            return (NamedDependentItemContainerRef)base.EnsureContextPrefix( defaultContextName );
        }

        /// <summary>
        /// Overriden to create a <see cref="NamedDependentItemContainerRef"/>.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?' but this is ignored: <paramref name="optional"/> drives the optionality.</param>
        /// <param name="optional">True for an optional reference.</param>
        /// <returns>A new <see cref="NamedDependentItemContainerRef"/> instance.</returns>
        protected override NamedDependentItemRef Create( string fullName, bool optional )
        {
            return new NamedDependentItemContainerRef( fullName, optional );
        }

        public static implicit operator NamedDependentItemContainerRef( string fullName )
        {
            return new NamedDependentItemContainerRef( fullName );
        }
    }

}
