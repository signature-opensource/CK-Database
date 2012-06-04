using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.Setup.SqlServer
{
    public class DatabaseSetupDriver : SetupDriverContainer
    {
        Func<string,SqlManager> _connectionProvider;
        SqlManager _manager;

        public DatabaseSetupDriver( BuildInfo info, Func<string,SqlManager> connectionProvider )
            : base( info )
        {
            _connectionProvider = connectionProvider;
        }

        public new Database Item
        {
            get { return (Database)base.Item; }
        }

        public SqlManager SqlManager
        {
            get { return _manager; }
        }

        protected override bool Init()
        {
            _manager = _connectionProvider( Item.Name );
            return true;
        }

    }
}
