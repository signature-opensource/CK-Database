#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\CK.Authentication.Local\Package .cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Setup;
using CK.SqlServer.Setup;

namespace CK.Authentication.Local
{
    [SqlPackage( Database = typeof(SqlDefaultDatabase), Schema="CK", ResourceType = typeof( Package ), ResourcePath = "Res" ), Versions( "1.0.0" )]
    public class Package : SqlPackage
    {
        [InjectContract]
        public UserHome UserHome { get; protected set; }
    }
}
