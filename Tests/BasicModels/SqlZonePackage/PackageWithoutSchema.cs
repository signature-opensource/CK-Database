using CK.Setup;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlZonePackage
{
    [SqlPackage( ResourcePath = "~SqlZonePackage.Res" ), Versions( "1.0.0" )]
    public abstract class PackageWithoutSchema : SqlPackage
    {
    }
}
