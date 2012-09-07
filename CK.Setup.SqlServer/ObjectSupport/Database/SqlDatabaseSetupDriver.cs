using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.Setup.SqlServer
{
    public class SqlDatabaseSetupDriver : StObjSetupDriver<SqlDatabase>
    {
        SqlManager _connection;

        public SqlDatabaseSetupDriver( BuildInfo info, ISqlManagerProvider sqlProvider )
            : base( info )
        {
            _connection = sqlProvider.FindManager( Object.Name );
        }

        protected override bool  Install()
        {
            foreach( var name in Object.Schemas )
            {
                _connection.ExecuteOneScript( String.Format( "if not exists(select 1 from sys.schemas where name = '{0}') begin exec( 'create schema {0}' ); end", name ), Engine.Logger );
            } 
            return true;
        }

    }
}
