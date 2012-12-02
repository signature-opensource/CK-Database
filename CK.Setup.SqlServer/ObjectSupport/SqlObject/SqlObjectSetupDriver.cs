using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlObjectSetupDriver : SetupDriver
    {
        readonly ISqlManagerProvider _provider;

        public SqlObjectSetupDriver( BuildInfo info, ISqlManagerProvider sqlProvider )
            : base( info )
        {
            if( sqlProvider == null ) throw new ArgumentNullException( "sqlProvider" );
            _provider = sqlProvider;
        }

        public new SqlObjectItem Item
        {
            get { return (SqlObjectItem)base.Item; }
        }

        protected override bool InstallContent()
        {
            if( ExternalVersion != null && ExternalVersion.Version == ((IVersionedItem)Item).Version ) return true;

            SqlManager m = SqlScriptTypeHandler.FindManagerFromLocation( Engine.Logger, _provider, FullName );
            if( m == null ) return false;
 
            string s;
            StringWriter w = new StringWriter();
            
            Item.WriteDrop( w );
            s = w.GetStringBuilder().ToString();
            if( !m.ExecuteOneScript( s, Engine.Logger ) ) return false;
            w.GetStringBuilder().Clear();
            
            Item.WriteCreate( w );
            s = w.GetStringBuilder().ToString();

            var tagHandler = new SimpleScriptTagHandler( s );
            if( !tagHandler.Expand( Engine.Logger, true ) ) return false;
            var scripts = tagHandler.SplitScript();
            if( !m.ExecuteScripts( scripts.Select( c => c.Body ), Engine.Logger ) ) return false;

            return true;
        }

    }
}
