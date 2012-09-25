using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Defines a named reference to an item.
    /// </summary>
    public class NamedDependentItemRef : IDependentItemRef
    {
        readonly string _fullName;
        readonly bool _optional;

        /// <summary>
        /// Initializes a new <see cref="NamedDependentItemRef"/> with a <see cref="FullName"/>
        /// optionaly starting with '?'.
        /// </summary>
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
