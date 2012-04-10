using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CK.Setup.Database.SqlServer
{
    public class SqlObjectDriver : SetupDriver
    {
        IDatabaseExecutor _db;

        public SqlObjectDriver( BuildInfo info, IDatabaseExecutor db )
            : base( info )
        {
            if( db == null ) throw new ArgumentNullException( "db" );
            _db = db;
        }

        public new SqlObject Item
        {
            get { return (SqlObject)base.Item; }
        }

        protected override bool Install()
        {
            if( ExternalVersion != null && ExternalVersion.Version == Item.Version ) return true;
            return _db.ExecuteScript( Item.WriteDrop, Item.WriteCreate );
        }

    }
}
