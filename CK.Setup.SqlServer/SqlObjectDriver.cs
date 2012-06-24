using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlObjectDriver : ItemDriver
    {
        SqlManager _manager;

        public SqlObjectDriver( BuildInfo info, SqlManager db )
            : base( info )
        {
            if( db == null ) throw new ArgumentNullException( "db" );
            _manager = db;
        }

        public new SqlObject Item
        {
            get { return (SqlObject)base.Item; }
        }

        protected override bool Install()
        {
            if( ExternalVersion != null && ExternalVersion.Version == ((IVersionedItem)Item).Version ) return true;

            string s;
            StringWriter w = new StringWriter();
            
            Item.WriteDrop( w );
            s = w.GetStringBuilder().ToString();
            if( !_manager.ExecuteOneScript( s, Engine.Logger ) ) return false;
            w.GetStringBuilder().Clear();
            
            Item.WriteCreate( w );
            s = w.GetStringBuilder().ToString();

            var tagHandler = new SimpleScriptTagHandler( s );
            if( !tagHandler.Expand( Engine.Logger, true ) ) return false;
            var scripts = tagHandler.SplitScript();
            if( !_manager.ExecuteScripts( scripts.Select( c => c.Body ), Engine.Logger ) ) return false;

            return true;
        }

    }
}
