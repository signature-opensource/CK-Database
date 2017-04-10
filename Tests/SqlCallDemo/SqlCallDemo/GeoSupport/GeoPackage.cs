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
using CK.Core;
using System.Reflection;
using Microsoft.SqlServer.Types;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res.Geo" ), Versions( "1.0.0" )]
    public abstract class GeoPackage : SqlPackage
    {
        void StObjConstruct()
        {
        }

        [SqlScalarFunction("fAreaFunction")]
        public abstract double Area( ISqlCallContext ctx, SqlGeography g );

    }
}
