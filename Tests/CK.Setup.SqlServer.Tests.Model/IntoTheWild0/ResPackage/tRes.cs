using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup;
using CK.Setup.SqlServer;

namespace IntoTheWild0
{

    [SqlTable( "tRes", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    [SqlObjectItem( "sResCreate" )]
    [SqlObjectItem( "sResRemove" )]
    public class tRes : SqlTable
    {
    }
}
