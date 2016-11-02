#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\FileSetupScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CK.Setup
{
    public class FileSetupScript : ISetupScript
    {
        string _cached;

        public FileSetupScript( ParsedFileName n, string scriptSource )
        {
            if( n == null ) throw new ArgumentNullException( "n" );
            if( string.IsNullOrWhiteSpace( scriptSource ) ) throw new ArgumentException( "Must be not null nor empty nor white space.", "scriptSource" );
            if( !(n.ExtraPath is string) || !Path.IsPathRooted( (string)n.ExtraPath ) ) throw new ArgumentException( "ParsedFileName.ExtraPath must be a rooted file path.", "n" );
            Name = n;
            ScriptSource = scriptSource;
        }

        public string ScriptSource { get; }

        public ParsedFileName Name { get; }

        public string GetScript()
        {
            if( _cached == null )
            {
                string path = Path.Combine( (string)Name.ExtraPath, Name.FileName );
                _cached = File.ReadAllText( path );
            }
            return _cached;
        }

        public override string ToString() => $@"{ScriptSource} script - {Name.ExtraPath}\\{Name.FileName}";

    }
}
