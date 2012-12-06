using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Setup;
using CK.Setup.SqlServer;

namespace CK.Authentication.Local
{
    [SqlPackage( ResourceType = typeof( Package ), Database = typeof( SqlDefaultDatabase ), ResourcePath = "Res", Schema = "CK" )]
    [Versions( "1.0.0" )]
    //[SetupName( "CK.Authentication.Local.Package" )]
    public class Package : SqlPackage
    {
    }
}
