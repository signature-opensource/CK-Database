using CK.Core;
using CK.SqlServer;

namespace SqlCallDemo
{
    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "19.01.14" )]
    public abstract class TransactionalPackage : SqlPackage
    {
        // The sTransactSetLevelNotWorking procedure sets the SERIALIZABLE level but the
        // change is scoped to the stored procedure!
        [SqlProcedure( "sTransactSetLevelNotWorking" )]
        public abstract string TransactSetLevelNotWorking( ISqlTransactionCallContext c );
    }
}
