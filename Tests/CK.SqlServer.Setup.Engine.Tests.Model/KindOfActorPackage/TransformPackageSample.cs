using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlActorPackage
{

    [SqlPackage( ResourcePath = "TransformPackageRes", Schema = "CK", Database = typeof( SqlDefaultDatabase ) )]
    [SqlObjectItem( "transform:sGroupDestroy" )]
    public class TransformPackageSample : SqlPackage
    {
        void Construct( SqlActorPackage.Basic.Package actorPackage )
        {
        }
    }
}
