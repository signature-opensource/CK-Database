using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlViewSetupDriver : SetupDriver
    {
        readonly ISqlManagerProvider _provider;

        public SqlViewSetupDriver( BuildInfo info, ISqlManagerProvider sqlProvider )
            : base( info )
        {
            if( sqlProvider == null ) throw new ArgumentNullException( "sqlProvider" );
            _provider = sqlProvider;
        }

        public new SqlViewItem Item { get { return (SqlViewItem)base.Item; } }


        protected override bool LoadScripts( IScriptCollector scripts )
        {
            string fileName = Item.Name + ".sql";
            string text = Item.ResourceLocation.GetString( fileName, false );
            SqlView v = Item.GetObject();
            if( text == null )
            {
                fileName = v.SchemaName + ".sql";
                text = Item.ResourceLocation.GetString( fileName, false );
            }
            if( text == null )
            {
                fileName = v.ViewName + ".sql";
                text = Item.ResourceLocation.GetString( fileName, false );
            }
            if( text == null )
            {
                Engine.Monitor.Error().Send( 
                    "Resource '{0}' not found (tried '{1}' and '{2}' and '{3}').", 
                    Item.Name, Item.Name + ".sql", v.SchemaName + ".sql", v.ViewName + ".sql" );
                return false;
            }

            Item.ProtoItem = SqlObjectParser.Create( Engine.Monitor, Item, text );
            return true;
        }

        protected override bool InstallContent()
        {
            if( ExternalVersion != null && ExternalVersion.Version == ((IVersionedItem)Item).Version ) return true;

            ISqlManager m = FindManagerFromLocation( Engine.Monitor, _provider, FullName );
            if( m == null ) return false;

            string s;
            StringWriter w = new StringWriter();

            Item.WriteDrop( w );
            s = w.GetStringBuilder().ToString();
            if( !m.ExecuteOneScript( s, Engine.Monitor ) ) return false;
            w.GetStringBuilder().Clear();

            Item.WriteCreate( w );
            s = w.GetStringBuilder().ToString();

            var tagHandler = new SimpleScriptTagHandler( s );
            if( !tagHandler.Expand( Engine.Monitor, true ) ) return false;
            var scripts = tagHandler.SplitScript();
            if( !m.ExecuteScripts( scripts.Select( c => c.Body ), Engine.Monitor ) ) return false;

            return true;
        }


        public static ISqlManager FindManagerFromLocation( IActivityMonitor monitor, ISqlManagerProvider provider, string fullName )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( provider == null ) throw new ArgumentNullException( "provider" );
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            ISqlManager m = null;
            string context, location, name;
            if( !DefaultContextLocNaming.TryParse( fullName, out context, out location, out name ) || String.IsNullOrEmpty( location ) )
            {
                monitor.Error().Send( "Unable to extract a location from FullName '{0}' in order to find a Sql connection.", fullName );
            }
            else if( (m = provider.FindManagerByName( location )) == null )
            {
                monitor.Error().Send( "Location '{0}' from FullName '{1}' can not be mapped to an existing Sql Connection.", location, fullName );
            }
            return m;
        }
    }
}
