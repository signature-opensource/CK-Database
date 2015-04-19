#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ContextLocName\ContextLocName.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Independent implementation of <see cref="IContextLocNaming"/> (wraps <see cref="ContextLocNameStructImpl"/> helper).
    /// This can be overriden to manage other constraints or naming conventions or sub parts.
    /// </summary>
    public class ContextLocName : IContextLocNaming
    {
        ContextLocNameStructImpl _impl;

        /// <summary>
        /// Initializes a new empty <see cref="ContextLocName"/>.
        /// </summary>
        public ContextLocName()
        {
            _impl = new ContextLocNameStructImpl();
        }

        /// <summary>
        /// Initializes a new <see cref="ContextLocName"/> with a full name.
        /// </summary>
        /// <param name="fullName">Initial full name.</param>
        public ContextLocName( string fullName )
        {
            _impl = new ContextLocNameStructImpl( fullName );
        }

        /// <summary>
        /// Initializes a new <see cref="ContextLocName"/> with context, location and name.
        /// Name must be not null.
        /// </summary>
        /// <param name="context">Can be null (unknown).</param>
        /// <param name="location">Can be null (unknown).</param>
        /// <param name="name">Can not be null.</param>
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
            set 
            { 
                if( _impl.Name != value )
                {
                    _impl.Name = value;
                    OnNameChanged();
                }
            }
        }

        /// <summary>
        /// Called whenever the <see cref="Name"/> has changed.
        /// </summary>
        protected virtual void OnNameChanged()
        {
        }

        /// <summary>
        /// Gets or sets the full name. 
        /// <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> are automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string FullName
        {
            get { return _impl.FullName; }
            set 
            {
                string oldName = _impl.Name;
                _impl.FullName = value;
                if( oldName != _impl.Name ) OnNameChanged();
            }
        }

        /// <summary>
        /// Overriden to return the <see cref="FullName"/>.
        /// </summary>
        /// <returns>The FullName of this name.</returns>
        public override string ToString()
        {
            return _impl.FullName;
        }
    }
}
