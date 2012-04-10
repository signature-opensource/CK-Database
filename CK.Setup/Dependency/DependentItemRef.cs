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
    public struct DependentItemRef : IDependentItemRef
    {
        string _fullName;

        /// <summary>
        /// Initializes a new <see cref="DependentItemRef"/> with a <see cref="FullName"/>.
        /// </summary>
        public DependentItemRef( string fullName )
        {
            _fullName = fullName ?? String.Empty;
        }

        /// <summary>
        /// Gets or sets a name that uniquely identifies a container. 
        /// It is automatically set to <see cref="String.Empty"/> when null is set.
        /// </summary>
        public string FullName 
        {
            get { return _fullName; }
            set { _fullName = value ?? _fullName; }
        }

        public override bool Equals( object obj )
        {
            return obj is DependentItemRef ? ((DependentItemRef)obj).FullName == _fullName : false;
        }

        public override int GetHashCode()
        {
            return _fullName.GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }

    }
}
