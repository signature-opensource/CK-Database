using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using CK.Setup.SqlServer;

namespace CK.Authentication.Local
{
    [SqlTable( "tUser", Package = typeof( Package )), Versions( "CK.tUser-Local=1.0.0, 2.12.10" )]
    [SqlObjectItem( "fCukeHashPassword, fUserReadInfo, sUserCanImpersonate, sUserPwdEncrypt, sUserPwdSet" )]
    public class UserHome : SqlActorPackage.Basic.UserHome
    {
    }
}
