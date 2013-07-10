using CK.Core;

namespace CK.SqlServer.Setup
{
    [RemoveDefaultContext]
    [AddContext( "dbHisto" )]
    public class SqlHistoDatabase : SqlDatabase, IAmbientContract
    {
        public SqlHistoDatabase()
            : base( "dbHisto" )
        {
            InstallCore = true;
        }

        void Construct( string connectionString )
        {
            ConnectionString = connectionString;
        }
        
    }
}
