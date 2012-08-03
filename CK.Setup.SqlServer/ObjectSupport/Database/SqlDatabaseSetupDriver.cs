using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.Setup.SqlServer
{
    public class SqlDatabaseSetupDriver : ContainerDriver
    {
        SqlConnectionSetupDriver _connection;

        public SqlDatabaseSetupDriver( BuildInfo info )
            : base( info )
        {
            _connection = (SqlConnectionSetupDriver)DirectDependencies[Item.SqlConnection];
        }

        public new SqlDatabaseItem Item
        {
            get { return (SqlDatabaseItem)base.Item; }
        }

        public SqlConnectionSetupDriver SqlConnection
        {
            get { return _connection; }
        }

        protected override bool Install()
        {
            foreach( var name in Item.Database.Schemas )
            {
                _connection.SqlManager.ExecuteOneScript( String.Format( "if not exists(select 1 from sys.schemas where name = '{0}') begin exec( 'create schema {0}' ); end", name ), Engine.Logger );
            } 
            return true;
        }

    }
}
