using CK.SqlServer;
using CK.Core;
using System;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class AllDefaultValuesPackage : SqlPackage
    {
        [SqlProcedure( "sAllDefaultValues" )]
        public abstract string AllDefaultValues( SqlStandardCallContext ctx );

        [SqlProcedure( "sAllDefaultValues" )]
        public abstract string AllDefaultValuesButTime( SqlStandardCallContext ctx, TimeSpan time );

    }
}
