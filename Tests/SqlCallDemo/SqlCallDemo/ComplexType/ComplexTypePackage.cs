using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo.ComplexType
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class ComplexTypePackage : SqlPackage
    {
        [SqlProcedureNonQuery( "sComplexTypeStupidEmpty" )]
        public abstract ComplexTypeStupidEmpty GetComplexTypeStupidEmpty( ISqlCallContext ctx );

        [SqlProcedureNonQuery( "sComplexTypeSimple" )]
        public abstract ComplexTypeSimple GetComplexTypeSimple( ISqlCallContext ctx, int id = 0 );

        [SqlProcedureNonQuery( "sComplexTypeSimple" )]
        public abstract ComplexTypeSimpleWithCtor GetComplexTypeSimpleWithCtor( ISqlCallContext ctx, int id = 0 );

        [SqlProcedureNonQuery( "sComplexTypeSimple" )]
        public abstract ComplexTypeSimpleWithExtraProperty GetComplexTypeSimpleWithExtraProperty( ISqlCallContext ctx, int id = 0 );

        [SqlProcedureNonQuery( "sComplexTypeSimple" )]
        public abstract ComplexTypeSimpleWithMissingProperty GetComplexTypeSimpleWithMissingProperty( ISqlCallContext ctx, int id = 0 );

    }
}
