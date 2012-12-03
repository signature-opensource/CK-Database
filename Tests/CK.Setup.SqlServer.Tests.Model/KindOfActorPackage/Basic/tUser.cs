using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlActorPackage.Basic
{
    [SqlTable( "tUser", Package = typeof( Package ) ), Versions( "2.11.25" )]
    [SqlObjectItem( "sUserCreate" )]
    public class tUser : SqlTable
    {
        void Construct( tActor actor )
        {
        }
    }
}
