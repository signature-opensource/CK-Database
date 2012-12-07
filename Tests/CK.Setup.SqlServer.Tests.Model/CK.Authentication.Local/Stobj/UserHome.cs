using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using CK.Setup.SqlServer;

namespace CK.Authentication.Local
{
    [SqlTable( "tUser", Package = typeof( Package )), Versions( "1.0.0" )]
    [SqlObjectItem( "fCukeHashPassword, fUserReadInfo, sUserCanImpersonate, sUserPwdEncrypt, sUserPwdSet" )]
    public class tUser : SqlActorPackage.Basic.tUser
    {
        //void Construct( SqlActorPackage.Basic.tUser baseUserHome )
        //{
        //}
    }
}
