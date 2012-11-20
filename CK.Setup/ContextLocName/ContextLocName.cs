using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Independent implementation of <see cref="IContextLocName"/> (wraps <see cref="ContextLocNameStructImpl"/> helper).
    /// </summary>
    public class ContextLocName : IContextLocName
    {
        ContextLocNameStructImpl _impl;

        /// <summary>
        /// Initializes a new empty <see cref="ContextLocName"/>.
        /// </summary>
        public ContextLocName()
        {
            _impl = new ContextLocNameStructImpl( null );
        }

        /// <summary>
        /// Initializes a new <see cref="ContextLocNameStructImpl"/> with a full name.
        /// </summary>
        /// <param name="fullName">Initial full name.</param>
        public ContextLocName( string fullName )
        {
            _impl = new ContextLocNameStructImpl( fullName );
        }

        /// <summary>
        /// Initializes a new <see cref="ContextLocNameStructImpl"/> with context, location and name.
        /// </summary>
        public ContextLocName( string context, string location, string name )
        {
            _impl = new ContextLocNameStructImpl( context, location, name );
        }

        /// <summary>
        /// Gets or sets the context part of this name.
        /// Can be null (unknown context) or empty (default context).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Context 
        {
            get { return _impl.Context; }
            set { _impl.Context = value; }
        }

        /// <summary>
        /// Gets or sets the location part of this name. 
        /// Can be null (unknown location) or empty (root location).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Location
        {
            get { return _impl.Location; }
            set { _impl.Location = value; }
        }

        /// <summary>
        /// Gets or sets the name part (without <see cref="Context"/> nor <see cref="Location"/>). 
        /// When set, <see cref="FullName"/> is automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string Name
        {
            get { return _impl.Name; }
            set { _impl.Name = value; }
        }

        /// <summary>
        /// Gets or sets the full name. 
        /// <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> are automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string FullName
        {
            get { return _impl.FullName; }
            set { _impl.FullName = value; }
        }
    }
}
