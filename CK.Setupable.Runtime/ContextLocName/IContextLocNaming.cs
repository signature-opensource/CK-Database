#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ContextLocName\IContextLocNaming.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// Defines the read only aspect of Context-Location-Name triplet.
/// </summary>
public interface IContextLocNaming
{
    /// <summary>
    /// Gets the context part of this name.
    /// Can be null (unknown context) or empty (default context).
    /// </summary>
    string Context { get; }

    /// <summary>
    /// Gets the location part of this name. 
    /// Can be null (unknown location) or empty (root location).
    /// </summary>
    string Location { get; }

    /// <summary>
    /// Gets the name part (without <see cref="Context"/> nor <see cref="Location"/>).
    /// Must not be null.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the full name composed of <see cref="Context"/>, <see cref="Location"/> and <see cref="Name"/>.
    /// Must not be null.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Gets the transform argument name (that is the suffix enclosed in parenthesis) if it exists, null otherwise. 
    /// This can be null (no target meanig that this is not a transformer's name) or not empty: an empty transformation 
    /// argument is not valid.
    /// </summary>
    string TransformArg { get; }

    /// <summary>
    /// Combines this <see cref="IContextLocNaming"/> with another one: if the other one has unknown <see cref="Context"/> 
    /// or <see cref="Location"/> those from this name are used (this also applies to the potential transform argument 
    /// of <paramref name="n"/>).
    /// </summary>
    /// <param name="n">The raw name. When null or empty, this name is cloned.</param>
    /// <returns>A new combined name.</returns>
    IContextLocNaming CombineName( string n );
}
