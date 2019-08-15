#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ContextLocName\IContextLocNaming.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Core
{
    /// <summary>
    /// Defines the immutable aspect of Context-Location-Name triplet.
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
        /// Gets the transform argument name: the suffix enclosed in parenthesis if it exists, null otherwise. 
        /// This can be null (no target) or not empty: an empty transformation argument is not valid.
        /// </summary>
        string TransformArg { get; }

    }
}
