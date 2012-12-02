using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup;
using CK.Setup.SqlServer;

namespace IntoTheWild0
{

    [SqlTable( "tResDataString", Package = typeof( ResPackage ) ), Versions( "2.9.2" )]
    [SqlObjectItem( "sResDataStringSet" )]
    [SqlObjectItem( "sResDataStringRemove" )]
    public class tResDataString : SqlTable
    {
        void Construct( tRes res )
        {
        }
    }
}
