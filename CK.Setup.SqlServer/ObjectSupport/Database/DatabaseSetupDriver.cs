using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.Setup.SqlServer
{
    public class DatabaseSetupDriver : ContainerDriver
    {
        Func<string,SqlManager> _connectionProvider;
        SqlManager _manager;

        public DatabaseSetupDriver( BuildInfo info, Func<string,SqlManager> connectionProvider )
            : base( info )
        {
            if( connectionProvider == null ) throw new ArgumentNullException( "connectionProvider" );
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

        protected override bool  Install()
        {
            _manager = _connectionProvider( Item.Name );
            foreach( var name in Item.Schemas )
            {
                _manager.ExecuteOneScript( String.Format( "if not exists(select 1 from sys.schemas where name = '{0}') begin exec( 'create schema {0}' ); end", name ), Engine.Logger );
            } 
            return true;
        }

    }
}
