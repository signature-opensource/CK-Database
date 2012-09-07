using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup;
using CK.Setup.SqlServer;

namespace IntoTheWild0
{

    [SqlTable( "tResDataString", Package = typeof( ResourcePackage ) ), Versions( "2.9.2" )]
    public class tResDataString : SqlTableType
    {
    }
}
