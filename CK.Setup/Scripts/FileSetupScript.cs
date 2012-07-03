using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CK.Setup
{
    public class FileSetupScript : ISetupScript
    {
        public FileSetupScript( ParsedFileName n, string scriptType )
        {
            if( n == null ) throw new ArgumentNullException( "n" );
            if( scriptType == null ) throw new ArgumentNullException( "scriptType" );
            if( !(n.ExtraPath is string) || !Path.IsPathRooted( (string)n.ExtraPath ) ) throw new ArgumentException( "ParsedFileName.ExtraPath must be a rooted file path.", "n" );
            Name = n;
            ScriptType = scriptType;
        }

        public string ScriptType { get; private set; }

        public ParsedFileName Name { get; private set; }

        public string GetScript()
        {
            string path = Path.Combine( (string)Name.ExtraPath, Name.FileName );
            return File.ReadAllText( path );
        }

        public override string ToString()
        {
            return String.Format( @"{0} script - {1}\\{2}", ScriptType, Name.ExtraPath, Name.FileName );
        }

    }
}
