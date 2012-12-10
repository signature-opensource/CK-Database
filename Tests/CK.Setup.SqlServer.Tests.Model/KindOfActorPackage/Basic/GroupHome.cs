using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;
using IntoTheWild0;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tGroup", Package = typeof( Package ) ), Versions( "CK.tGroup=2.12.9, 2.12.10" )]
    [SqlObjectItem( "sGroupCreate" )]
    public class GroupHome : SqlTable
    {
        void Construct( ActorHome actor )
        {
        }
    }
}
