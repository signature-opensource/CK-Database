using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CK.Setup
{
    public class FileSetupScript : ISetupScript
    {
        public FileSetupScript( ParsedFileName n, string scriptSource )
        {
            if( n == null ) throw new ArgumentNullException( "n" );
            if( String.IsNullOrWhiteSpace(scriptSource) ) throw new ArgumentException( "Must be not null nor empty nor white space.", "scriptSource" );
            if( !(n.ExtraPath is string) || !Path.IsPathRooted( (string)n.ExtraPath ) ) throw new ArgumentException( "ParsedFileName.ExtraPath must be a rooted file path.", "n" );
            Name = n;
            ScriptSource = scriptSource;
        }

        public string ScriptSource { get; private set; }

        public ParsedFileName Name { get; private set; }

        public string GetScript()
        {
            string path = Path.Combine( (string)Name.ExtraPath, Name.FileName );
            return File.ReadAllText( path );
        }

        public override string ToString()
        {
            return String.Format( @"{0} script - {1}\\{2}", ScriptSource, Name.ExtraPath, Name.FileName );
        }

    }
}
