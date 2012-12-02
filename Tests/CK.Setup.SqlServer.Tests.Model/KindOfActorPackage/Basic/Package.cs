using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup.SqlServer;
using CK.Setup;

namespace SqlActorPackage.Basic
{

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourceType = typeof( Package ), ResourcePath = "Res" ), Versions( "2.11.25" )]
    public class Package : SqlPackage
    {
    }
}
