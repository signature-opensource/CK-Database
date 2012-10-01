using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using CK.Setup.SqlServer;

namespace IntoTheWild0
{
    [SqlPackage( Schema = "CK", Database = typeof(SqlDefaultDatabase), ResourceType=typeof(ResPackage), ResourcePath="ResPackage.Resource" ), Versions( "2.9.2" )]
    public class ResPackage : SqlPackage
    {
    }

}
