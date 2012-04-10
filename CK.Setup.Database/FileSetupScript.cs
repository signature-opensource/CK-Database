using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CK.Setup.Database
{
    public class FileSetupScript : ISetupScript
    {
        public FileSetupScript( ParsedFileName n, string scriptType )
        {
            Name = n;
            ScriptType = scriptType;
        }

        public string ScriptType { get; private set; }

        public ParsedFileName Name { get; private set; }

        public string GetScript()
        {
            string path = Path.Combine( Name.ExtraPath, Name.FileName );
            return File.ReadAllText( path );
        }

    }
}
