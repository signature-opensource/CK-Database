using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Implements a named reference to an item.
    /// </summary>
    public class NamedDependentItemRef : IDependentItemNamedRef
    {
        readonly string _fullName;
        readonly bool _optional;

        /// <summary>
        /// Initializes a new <see cref="NamedDependentItemRef"/> with a <see cref="FullName"/>
        /// optionaly starting with '?'.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?'.</param>
        public NamedDependentItemRef( string fullName )
        {
            if( String.IsNullOrWhiteSpace( fullName ) ) throw new ArgumentException( "Must not be a not null nor empty nor whitespace string.", "fullName" );
            _fullName = fullName;
            _optional = false;
            if( fullName[0] == '?' )
            {
                _fullName = fullName.Substring( 1 );
                _optional = true;
            }
        }

        /// <summary>
        /// Initializes a potentially optional new <see cref="NamedDependentItemRef"/> with a <see cref="FullName"/>.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?' but this is ignored: <paramref name="optional"/> drives the optionality.</param>
        /// <param name="optional">True for an optional reference.</param>
        public NamedDependentItemRef( string fullName, bool optional )
            : this( fullName )
        {
            _optional = optional;
        }

        /// <summary>
        /// Gets the name that uniquely identifies an item. 
        /// </summary>
        public string FullName 
        {
            get { return _fullName; }
        }

        public bool Optional
        {
            get { return _optional; }
        }

        /// <summary>
        /// Explicit implementation that relays to public covariant <see cref="EnsureContextPrefix"/> method that itself 
        /// uses protected virtual <see cref="Create"/> to be able to return specialized types.
        /// </summary>
        IDependentItemNamedRef IDependentItemNamedRef.DoEnsureContextPrefix( string defaultContextName )
        {
            return EnsureContextPrefix( defaultContextName );
        }

        /// <summary>
        /// Makes sure that this <see cref="FullName"/> has a [context] prefix (and returns this instance) or
        /// creates a new <see cref="NamedDependentItemRef"/> (or a more specialized type) with the given prefix.
        /// </summary>
        /// <param name="defaultContextName">Context name to inject if no context prefix exists.</param>
        /// <returns>This instance or a new prefixed one.</returns>
        /// <remarks>
        /// This implementation is not virtual to offer return type covariance.
        /// It calls protected virtual <see cref="Create"/> to be able to return specialized types.
        /// </remarks>
        public NamedDependentItemRef EnsureContextPrefix( string defaultContextName )
        {
            string newName = ContextNaming.SetContextPrefixIfNotExists( _fullName, defaultContextName );
            if( ReferenceEquals( newName, _fullName ) ) return this;
            return Create( newName, _optional );
        }

        /// <summary>
        /// Kind of "virtual constructor" to create an instance of the same type.
        /// </summary>
        /// <param name="fullName">Full name of the object. May start with '?' but this is ignored: <paramref name="optional"/> drives the optionality.</param>
        /// <param name="optional">True for an optional reference.</param>
        /// <returns>A new instance.</returns>
        protected virtual NamedDependentItemRef Create( string fullName, bool optional )
        {
            return new NamedDependentItemRef( fullName, _optional );
        }

        public static implicit operator NamedDependentItemRef( string fullName )
        {
            return new NamedDependentItemRef( fullName );
        }

        public override bool Equals( object obj )
        {
            if( obj is IDependentItemRef )
            {
                IDependentItemRef o = (IDependentItemRef)obj;
                return o.Optional == Optional && o.FullName == _fullName;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = _fullName.GetHashCode();
            if( _optional ) h = -h;
            return h;
        }

        public override string ToString()
        {
            return FullName;
        }

    }
}
