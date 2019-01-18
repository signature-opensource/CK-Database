using CK.Setup;
using CK.SqlServer.Setup;
using CK.SqlServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlCallDemo
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "19.01.14" )]
    public abstract class TransactionalPackage : SqlPackage
    {
        // This sets the SERIALIZABLE level but the change is scoped to the stored procedure.
        [SqlProcedure( "sTransactSetLevelNotWorking" )]
        public abstract string TransactSetLevelNotWorking( ISqlTransactionCallContext c );
    }
}
