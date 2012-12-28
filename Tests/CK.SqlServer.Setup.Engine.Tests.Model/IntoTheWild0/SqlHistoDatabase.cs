using CK.Core;

namespace CK.SqlServer.Setup
{
    [RemoveDefaultContext]
    [AddContext( "dbHisto" )]
    public class SqlHistoDatabase : SqlDatabase, IAmbientContract
    {
        public void Construct( string connectionString = null )
        {
            ConnectionString = connectionString;
            Name = "dbHisto";
            InstallCore = true;
        }
        
    }
}
