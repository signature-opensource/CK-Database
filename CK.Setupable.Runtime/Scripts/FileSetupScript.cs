#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\FileSetupScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.IO;

namespace CK.Setup
{
    /// <summary>
    /// File based implementation of <see cref="ISetupableAspect"/>.
    /// </summary>
    public class FileSetupScript : ISetupScript
    {
        string _cached;

        /// <summary>
        /// Initialized a new <see cref="FileSetupScript"/>.
        /// </summary>
        /// <param name="n">The name.</param>
        public FileSetupScript( ParsedFileName n )
        {
            if( n == null ) throw new ArgumentNullException( "n" );
            if( !(n.ExtraPath is string) || !Path.IsPathRooted( (string)n.ExtraPath ) ) throw new ArgumentException( "ParsedFileName.ExtraPath must be a rooted file path.", "n" );
            Name = n;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public ParsedFileName Name { get; }

        /// <summary>
        /// Reads the file content from <see cref="ParsedFileName.ExtraPath"/>/<see cref="ParsedFileName.Name"/>.
        /// </summary>
        /// <returns>The file content.</returns>
        public string GetScript()
        {
            if( _cached == null )
            {
                string path = Path.Combine( (string)Name.ExtraPath, Name.FileName );
                _cached = File.ReadAllText( path );
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
