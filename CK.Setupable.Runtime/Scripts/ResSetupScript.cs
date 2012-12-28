using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CK.Core;

namespace CK.Setup
{
    public class ResSetupScript : ISetupScript
    {
        public ResSetupScript( ParsedFileName n, string scriptSource )
        {
            if( n == null ) throw new ArgumentNullException( "n" );
            if( String.IsNullOrWhiteSpace(scriptSource) ) throw new ArgumentException( "Must be not null nor empty nor white space.", "scriptSource" );
            if( !(n.ExtraPath is ResourceLocator) ) throw new ArgumentException( "ParsedFileName.ExtraPath must be a ResourceLocator.", "n" );
            Name = n;
            ScriptSource = scriptSource;
        }

        public string ScriptSource { get; private set; }

        public ParsedFileName Name { get; private set; }

        public string GetScript()
        {
            ResourceLocator resLoc = (ResourceLocator)Name.ExtraPath;
            return resLoc.GetString( Name.FileName, true );
        }

        public override string ToString()
        {
            return String.Format( @"{0} script - {1}\\{2}", ScriptSource, Name.ExtraPath, Name.FileName );
        }

    }
}
