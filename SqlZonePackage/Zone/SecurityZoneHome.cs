using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlZonePackage.Zone
{
    [SqlTable( "tSecurityZone", Package = typeof(Package) ), Versions( "CK.tSecurityZone=2.11.25, 2.12.10" ) ]
    public class SecurityZoneHome : SqlTable
    {
        void Construct( SqlActorPackage.Basic.GroupHome group )
        {
        }
    }
}
