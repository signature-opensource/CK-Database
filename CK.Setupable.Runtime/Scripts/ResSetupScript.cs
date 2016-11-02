#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Scripts\ResSetupScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
        string _cached;

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
            if( _cached == null )
            {
                ResourceLocator resLoc = (ResourceLocator)Name.ExtraPath;
                _cached = resLoc.GetString( Name.FileName, true );
            }
            return _cached;
        }

        public override string ToString() => $@"{ScriptSource} script - {Name.ExtraPath}\\{Name.FileName}";

    }
}
