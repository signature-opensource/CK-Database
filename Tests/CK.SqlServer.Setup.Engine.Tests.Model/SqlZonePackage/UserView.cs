using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer.Setup;

namespace SqlZonePackage.Zone
{
    [SqlView( "vUser", Package = typeof( Zone.Package ) )]
    public class UserView : SqlActorPackage.Basic.UserView
    {
        void Construct( SecurityZoneHome zoneHome )
        {
        }
    }
}
