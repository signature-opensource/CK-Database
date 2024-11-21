#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ContextLocName\ContextLocNameStructImpl.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup;

/// <summary>
/// Encapsulates <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/> synchronized with the <see cref="FullName"/>.
/// This is defined as a struct in order to be included in classes without new object allocation: it should not be exposed as-is
/// (and the fact that it does not implement <see cref="IContextLocNaming"/> is done on purpose).
/// Note that one of the constructor should be called explicitly.
/// </summary>
public struct ContextLocNameStructImpl
{
    string _fullName;
    string _context;
    string _location;
    string _name;
    string _transformArg;

    /// <summary>
    /// Initializes a new <see cref="ContextLocNameStructImpl"/> with a full name.
    /// </summary>
    /// <param name="fullName">Initial full name. (Note: There is no default constructor for struct, this ctor should be called with (string)null or String.Empty.)</param>
    public ContextLocNameStructImpl( string fullName )
    {
        // Initialize class invariants.
        _fullName = _name = String.Empty;
        _context = _location = null;
        _transformArg = null;
        FullName = fullName;
    }

    /// <summary>
    /// Initializes a new <see cref="ContextLocNameStructImpl"/> with a triplet.
    /// </summary>
    /// <param name="context">The context string. Can be null.</param>
    /// <param name="location">The location. Can be null.</param>
    /// <param name="name">The name, may be suffixed with the (<see cref="TransformArg"/>). Can not be null.</param>
    public ContextLocNameStructImpl( string context, string location, string name )
    {
        if( name == null ) throw new ArgumentNullException( "name" );
        _context = context;
        _location = location;
        _name = name;
        int len = name.Length;
        _transformArg = DefaultContextLocNaming.ExtractTransformArg( name, 0, ref len );
        if( len != name.Length ) _name = name.Substring( 0, len );
        else _name = name;
        _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
    }

    /// <summary>
    /// Initializes a new <see cref="ContextLocNameStructImpl"/> with a context, location and base name plus
    /// the transform argument.
    /// </summary>
    /// <param name="context">The context string. Can be null.</param>
    /// <param name="location">The location. Can be null.</param>
    /// <param name="nameWithoutTransformArg">The name. Can not be null.</param>
    /// <param name="transformArg">The transform argument. Can not be null nor empty.</param>
    public ContextLocNameStructImpl( string context, string location, string nameWithoutTransformArg, string transformArg )
    {
        if( nameWithoutTransformArg == null ) throw new ArgumentNullException( nameof( nameWithoutTransformArg ) );
        if( string.IsNullOrEmpty( transformArg ) ) throw new ArgumentNullException( nameof( transformArg ) );
        _context = context;
        _location = location;
        _name = nameWithoutTransformArg + "(" + transformArg + ")";
        _transformArg = transformArg;
        _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
    }

    /// <summary>
    /// Initializes a new <see cref="ContextLocNameStructImpl"/> from a non null <see cref="IContextLocNaming"/>.
    /// </summary>
    /// <param name="contextLocName">The existing name.</param>
    public ContextLocNameStructImpl( IContextLocNaming contextLocName )
    {
        if( contextLocName == null ) throw new ArgumentNullException( "contextLocName" );
        _context = contextLocName.Context;
        _location = contextLocName.Location;
        _name = contextLocName.Name;
        _fullName = contextLocName.FullName;
        _transformArg = contextLocName.TransformArg;
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
    /// Gets or sets the name. <see cref="FullName"/> and <see cref="TransformArg"/> are automatically updated.
    /// Never null (normalized to <see cref="String.Empty"/>).
    /// </summary>
    public string Name
    {
        get { return _name; }
        set
        {
            if( value == null ) value = string.Empty;
            if( _name != value )
            {
                _name = value;
                int len = _name.Length;
                _transformArg = DefaultContextLocNaming.ExtractTransformArg( _name, 0, ref len );
                if( len != _name.Length ) _name = _name.Substring( 0, len );
                _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
            }
        }
    }

    /// <summary>
    /// Gets or sets the tranformation argument full name. 
    /// The <see cref="Name"/> and <see cref="FullName"/> are updated.
    /// This can be null (no target) or not empty: an empty transformation argument is not valid.
    /// </summary>
    public string TransformArg
    {
        get { return _transformArg; }
        set
        {
            if( value != null && value.Length == 0 ) throw new ArgumentException( "TransformArg can not be empty (but can be null)." );
            if( _transformArg != value )
            {
                if( _transformArg != null ) _name = DefaultContextLocNaming.RemoveTransformArg( _name, 0, _name.Length );
                if( value != null ) _name = DefaultContextLocNaming.AppendTransformArg( _name, value );
                _transformArg = value;
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
                    _context = _location = _transformArg = null;
                }
                else
                {
                    if( !DefaultContextLocNaming.TryParse( value, out _context, out _location, out _name, out _transformArg ) )
                    {
                        _fullName = _name = value;
                    }
                    else
                    {
                        _fullName = DefaultContextLocNaming.Format( _context, _location, _name );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Combines this name with another one: if the other one has unknown <see cref="Context"/> or <see cref="Location"/> 
    /// those from this name are used (this also applies to the potential transform argument of <paramref name="n"/>).
    /// </summary>
    /// <param name="n">The raw name. When null or empty, this name is cloned.</param>
    /// <returns>A new combined name.</returns>
    public ContextLocNameStructImpl CombineName( string n )
    {
        if( String.IsNullOrEmpty( n ) ) return this;
        var name = new ContextLocNameStructImpl( n );
        if( name.Context == null ) name.Context = Context;
        if( name.Location == null ) name.Location = Location;
        // Now handling transformation.
        if( name.TransformArg != null )
        {
            // The provided name is a transformation: resolves context/location/schema from container 
            // on the target component if they are not define.
            var target = new ContextLocNameStructImpl( name.TransformArg );
            if( target.Context == null ) target.Context = name.Context;
            if( target.Location == null ) target.Location = name.Location;
            name.TransformArg = target.FullName;
        }
        return name;
    }

    /// <summary>
    /// Overridden to return the hash of the full name.
    /// </summary>
    /// <returns>Hash of the full name.</returns>
    public override int GetHashCode() => _fullName != null ? _fullName.GetHashCode() : 0;

    /// <summary>
    /// Equality is bound to the <see cref="FullName"/>.
    /// </summary>
    /// <param name="obj">Object to compare to.</param>
    /// <returns>True if FullName are equal.</returns>
    public override bool Equals( object obj )
    {
        IContextLocNaming i = obj as IContextLocNaming;
        if( i != null ) return i.FullName == _fullName;
        if( obj is ContextLocNameStructImpl )
        {
            ContextLocNameStructImpl s = (ContextLocNameStructImpl)obj;
            return s.FullName == _fullName;
        }
        return base.Equals( obj );
    }

    /// <summary>
    /// Returns this <see cref="FullName"/>, mainly for debugging purposes.
    /// </summary>
    /// <returns>This FullName.</returns>
    public override string ToString() => _fullName;

}
