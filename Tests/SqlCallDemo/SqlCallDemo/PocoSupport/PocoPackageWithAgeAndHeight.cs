using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res.Poco" ), Versions( "1.0.0" )]
    [SqlObjectItem( "transform:sPocoThingWrite" )]
    public class PocoPackageWithAgeAndHeight : SqlPackage
    {
        void StObjConstruct( PocoPackage p )
        {
        }
    }
}
