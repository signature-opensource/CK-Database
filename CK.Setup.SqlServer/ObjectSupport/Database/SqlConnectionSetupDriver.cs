using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.Setup.SqlServer
{
    public class SqlConnectionSetupDriver : ItemDriver
    {
        Func<string,SqlManager> _connectionProvider;
        SqlManager _manager;

        public SqlConnectionSetupDriver( BuildInfo info, Func<string, SqlManager> connectionProvider )
            : base( info )
        {
            if( connectionProvider == null ) throw new ArgumentNullException( "connectionProvider" );
            _connectionProvider = connectionProvider;
        }

        public new SqlConnectionItem Item
        {
            get { return (SqlConnectionItem)base.Item; }
        }

        public SqlManager SqlManager
        {
            get { return _manager; }
        }

        protected override bool Init()
        {
            _manager = _connectionProvider( Item.SqlDatabase.Database.Name );
            return true;
        }

    }
}
