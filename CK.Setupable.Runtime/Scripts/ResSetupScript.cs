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

        public ResSetupScript( ParsedFileName n )
        {
            if( n == null ) throw new ArgumentNullException( "n" );
            if( !(n.ExtraPath is ResourceLocator) ) throw new ArgumentException( "ParsedFileName.ExtraPath must be a ResourceLocator.", "n" );
            Name = n;
        }

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

        public override string ToString() => $@"Script - {Name.ExtraPath}\\{Name.FileName}";

    }
}
