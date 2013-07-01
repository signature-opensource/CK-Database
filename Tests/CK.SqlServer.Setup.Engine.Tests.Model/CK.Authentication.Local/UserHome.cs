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
