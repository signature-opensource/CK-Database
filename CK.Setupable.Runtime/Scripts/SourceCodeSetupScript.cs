#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\FileSetupScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Runtime.CompilerServices;

namespace CK.Setup
{
    /// <summary>
    /// Source code implementation of <see cref="ISetupScript"/>.
    /// </summary>
    public class SourceCodeSetupScript : ISetupScript
    {
        string _script;

        SourceCodeSetupScript( ParsedFileName n, string script )
        {
            Name = n;
            _script = script;
        }

        /// <summary>
        /// Creates a <see cref="SourceCodeSetupScript"/> directly from source code.
        /// </summary>
        /// <param name="locName">The context-location-name for which a a <see cref="SourceCodeSetupScript"/> must be created.</param>
        /// <param name="script">The script content.</param>
        /// <param name="extension">The extension must not be null, empty or starts with a '.' dot.</param>
        /// <param name="step">Optional step (when no <paramref name="version"/> is supplied, this is a "no version" script).</param>
        /// <param name="fromVersion">
        /// Optional starting version for a migration script. 
        /// When not null, <paramref name="version"/> must be supplied.
        /// </param>
        /// <param name="version">Optional version of the script.</param>
        /// <param name="file">Automatically set the file source name. The path is ignored: only the filename with its extension is kept.</param>
        /// <param name="line">Automatically set the source line number.</param>
        /// <returns>A setup script.</returns>
        public static SourceCodeSetupScript CreateFromSourceCode( 
            IContextLocNaming locName, 
            string script, 
            string extension, 
            SetupCallGroupStep step = SetupCallGroupStep.None,
            Version fromVersion = null,
            Version version = null,
            [CallerFilePath]string file = null, 
            [CallerLineNumber] int line = 0 )
        {
            if( script == null ) throw new ArgumentNullException( nameof(script) );
            ParsedFileName name = ParsedFileName.CreateFromSourceCode( locName, extension, step, fromVersion, version, file, line );
            return new SourceCodeSetupScript( name, script );
        }

        /// <summary>
        /// Gets the name: built from the path of the source code file and the line number of the call
        /// to <see cref="CreateFromSourceCode(IContextLocNaming, string, string, SetupCallGroupStep, Version, Version, string, int)"/>.
        /// </summary>
        public ParsedFileName Name { get; }

        /// <summary>
        /// Gets the script content.
        /// </summary>
        /// <returns>The script.</returns>
        public string GetScript() => _script;

        /// <summary>
        /// Overridden to return a "Inline Script" with the <see cref="ParsedFileName.FileName"/>.
        /// </summary>
        /// <returns>The path and name.</returns>
        public override string ToString() => $@"Inline script - {Name.FileName}";

    }
}
