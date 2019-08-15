#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\ResSetupScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Resource based implementation of <see cref="ISetupScript"/>.
    /// </summary>
    public class ResSetupScript : ISetupScript
    {
        string _cached;

        /// <summary>
        /// Initializes a new <see cref="ResSetupScript"/>.
        /// </summary>
        /// <param name="n">The name.</param>
        public ResSetupScript( ParsedFileName n )
        {
            if( n == null ) throw new ArgumentNullException( "n" );
            if( !(n.ExtraPath is ResourceLocator) ) throw new ArgumentException( "ParsedFileName.ExtractPath must be a ResourceLocator.", "n" );
            Name = n;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public ParsedFileName Name { get; }

        /// <summary>
        /// Reads the resource content based on <see cref="ParsedFileName.ExtraPath"/> that is a <see cref="ResourceLocator"/>
        /// and the <see cref="ParsedFileName.FileName"/>.
        /// </summary>
        /// <returns>The resource as a string.</returns>
        public string GetScript()
        {
            if( _cached == null )
            {
                ResourceLocator resLoc = (ResourceLocator)Name.ExtraPath;
                _cached = resLoc.GetString( Name.FileName, true );
            }
            return _cached;
        }

        /// <summary>
        /// Overridden to return the path and name.
        /// </summary>
        /// <returns>The path and name.</returns>
        public override string ToString() => $@"Script - {Name.ExtraPath}\\{Name.FileName}";

    }
}
