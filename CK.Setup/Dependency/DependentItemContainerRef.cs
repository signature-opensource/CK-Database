using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Defines a named reference to a container.
    /// </summary>
    public struct DependentItemContainerRef : IDependentItemContainerRef
    {
        string _fullName;

        /// <summary>
        /// Initializes a new <see cref="DependentItemContainerRef"/> with a <see cref="FullName"/>.
        /// </summary>
        public DependentItemContainerRef( string fullName )
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
            return obj is DependentItemContainerRef ? ((DependentItemContainerRef)obj).FullName == _fullName : false;
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
