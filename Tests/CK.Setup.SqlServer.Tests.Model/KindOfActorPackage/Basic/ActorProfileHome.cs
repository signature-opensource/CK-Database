using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tActorProfile", Package = typeof( Package ) ), Versions( "2.12.9" )]
    public class ActorProfileHome : SqlTable
    {
        void Construct( ActorHome actor, GroupHome group )
        {
        }
    }
}
