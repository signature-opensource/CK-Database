using System;
using CK.SqlServer;

namespace CK.Setup.SqlServer
{
    public class SchemaSetupDriver : SetupDriver
    {
        SqlManager _manager;

        public SchemaSetupDriver( BuildInfo info, SqlManager db )
            : base( info )
        {
            if( db == null ) throw new ArgumentNullException( "db" );
            _manager = db;
        }

        public new Schema Item
        {
            get { return (Schema)base.Item; }
        }

        protected override bool Install()
        {
            return _manager.ExecuteOneScript( String.Format( "if not exists(select 1 from sys.schemas where name = '{0}') begin exec( 'create schema {0}' ); end", Item.FullName ), Engine.Logger );
        }
    }
}
