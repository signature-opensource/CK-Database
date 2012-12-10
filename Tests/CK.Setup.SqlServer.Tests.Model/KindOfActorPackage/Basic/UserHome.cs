using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tUser", Package = typeof( Package ) ), Versions( "CK.tUser=2.12.9, 2.12.10" )]
    [SqlObjectItem( "sUserCreate" )]
    public class UserHome : SqlTable
    {
        void Construct( ActorHome actor )
        {
        }
    }
}
