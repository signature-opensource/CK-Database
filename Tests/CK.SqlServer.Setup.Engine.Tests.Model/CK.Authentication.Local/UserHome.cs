#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\CK.Authentication.Local\UserHome.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Setup;
using CK.SqlServer.Setup;

namespace CK.Authentication.Local
{
    [SqlTable( "tUser", Package = typeof( Package )), Versions( "CK.tUser-Local=1.0.0, 2.12.10" )]
    [SqlObjectItem( "fCukeHashPassword, fUserReadInfo, sUserCanImpersonate, sUserPwdEncrypt, sUserPwdSet" )]
    public abstract class UserHome : SqlActorPackage.Basic.UserHome
    {
    }
}
