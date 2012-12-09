using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tGroup", Package = typeof( Package ) ), Versions( "2.11.25" ) ]
    public class GroupHome : SqlActorPackage.Basic.GroupHome
    {
        void Construct( SecurityZoneHome zone )
        {
        }
    }
}
