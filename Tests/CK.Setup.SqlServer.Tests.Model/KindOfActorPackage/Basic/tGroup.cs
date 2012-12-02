using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;
using IntoTheWild0;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tGroup", Package = typeof( Package ) ), Versions( "2.11.25" )]
    public class tGroup : SqlTable
    {
        void Construct( tActor actor )
        {
        }
    }
}
