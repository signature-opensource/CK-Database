#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\KindOfActorPackage\Basic\ActorHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using CK.Setup;
using CK.SqlServer.Setup;
using CK.Text;

namespace SqlActorPackage.Basic
{
    [SqlTable( "bad name table", Package = typeof( Package ), Schema = "bad schema name" )]
    [Versions( "1.0.0" )]
    public abstract class BadNameTable : SqlTable
    {

        public string JustToForceKeeptingTheReferencedCKTextAssembly()
        {
            return new[] { "a", "b" }.Concatenate();
        }
    }
}
