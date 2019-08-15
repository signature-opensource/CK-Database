#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\FileSetupScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup
{
    /// <summary>
    /// Direct implementation of <see cref="ISetupScript"/>: the script exists as a string.
    /// </summary>
    public class SetupScript : ISetupScript
    {
        readonly string _script;

        /// <summary>
        /// Initializes a new Setup script.
        /// </summary>
        /// <param name="name">The script name.</param>
        /// <param name="script">The script body.</param>
        public SetupScript( ParsedFileName name, string script )
        {
            if( name == null ) throw new ArgumentNullException( nameof(name) );
            if( script == null ) throw new ArgumentNullException( nameof(script) );
            Name = name;
            _script = script;
        }

        /// <summary>
        /// Gets the script name. Never null.
        /// </summary>
        public ParsedFileName Name { get; }

        /// <summary>
        /// Gets the script text. Never null.
        /// </summary>
        /// <returns>The script.</returns>
        public string GetScript() => _script;

        /// <summary>
        /// Overridden to return the extra path and filename of the <see cref="Name"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $@"Script - {Name.ExtraPath}\\{Name.FileName}";

    }
}
