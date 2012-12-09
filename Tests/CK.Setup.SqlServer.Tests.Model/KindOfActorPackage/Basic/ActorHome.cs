using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tActor", Package = typeof( Package ) ), Versions( "2.12.9" )]
    [SqlObjectItem( "sActorCreate" )]
    public class ActorHome : SqlTable
    {
    }
}
