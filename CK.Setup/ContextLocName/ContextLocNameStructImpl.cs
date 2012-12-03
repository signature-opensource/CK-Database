using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> synchronized with the <see cref="FullName"/>.
    /// This is defined as a struct in order to be included in classes without new object allocation: it should not be exposed as-is.
    /// Note that one of the constructor should be called explicitely.
    /// </summary>
    public struct ContextLocNameStructImpl
    {
        string _fullName;
        string _context;
        string _location;
        string _name;

        /// <summary>
        /// Initializes a new <see cref="ContextLocNameStructImpl"/> with a full name.
        /// </summary>
        /// <param name="fullName">Initial full name. (Note: There is no default constructor for struct, this ctor should be called with (string)null or String.Empty.)</param>
        public ContextLocNameStructImpl( string fullName )
        {
            // Initialize class invariants.
            _fullName = _name = String.Empty;
            _context = _location = null;
            FullName = fullName;
        }

        /// <summary>
        /// Initializes a new <see cref="ContextLocNameStructImpl"/> with a full name.
        /// </summary>
        public ContextLocNameStructImpl( string context, string location, string name )
        {
            if( name == null ) throw new ArgumentNullException( "name" );
            _context = context;
            _location = location;
            _name = name;
            _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
        }

        /// <summary>
        /// Initializes a new <see cref="ContextLocNameStructImpl"/> from a non null <see cref="IContextLocNaming"/>.
        /// </summary>
        /// <param name="contextLocName"></param>
        public ContextLocNameStructImpl( IContextLocNaming contextLocName )
        {
            if( contextLocName == null ) throw new ArgumentNullException( "contextLocName" );
            _context = contextLocName.Context;
            _location = contextLocName.Location;
            _name = contextLocName.Name;
            _fullName = contextLocName.FullName;
        }

        /// <summary>
        /// Gets or sets the context identifier. 
        /// Can be null (unknown context) or empty (the default context).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Context
        {
            get { return _context; }
            set
            {
                if( _context != value )
                {
                    _context = value;
                    _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
                }
            }
        }

        /// <summary>
        /// Gets or sets the location. 
        /// Can be null (unknown location) or empty (the root location).
        /// When set, <see cref="FullName"/> is automatically updated.
        /// </summary>
        public string Location
        {
            get { return _location; }
            set
            {
                if( _location != value )
                {
                    _location = value;
                    _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
                }
            }
        }

        /// <summary>
        /// Gets or sets the name. <see cref="FullName"/> is automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if( value == null ) value = String.Empty;
                if( _name != value )
                {
                    _name = value;
                    _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
                }
            }
        }

        /// <summary>
        /// Gets or sets the full name. 
        /// <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> are automatically updated.
        /// Never null (normalized to <see cref="String.Empty"/>).
        /// </summary>
        public string FullName
        {
            get { return _fullName; }
            set
            {
                if( _fullName != value )
                {
                    if( value == null )
                    {
                        _fullName = _name = String.Empty;
                        _context = _location = null;
                    }
                    else
                    {
                        if( !DefaultContextLocNaming.TryParse( value, out _context, out _location, out _name ) )
                        {
                            _fullName = _name = value;
                            _context = _location = null;
                        }
                        else
                        {
                            _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
                        }
                    }
                }
            }
        }
    }

}
