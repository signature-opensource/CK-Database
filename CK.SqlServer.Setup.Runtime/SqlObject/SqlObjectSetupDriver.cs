using System;
using System.IO;
using System.Linq;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
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

            SqlManager m = FindManagerFromLocation( Engine.Logger, _provider, FullName );
            if( m == null ) return false;
 
            string s;
            StringWriter w = new StringWriter();

            IDisposable configRestorer = null;
            bool itemMissingDependencyIsError = Item.MissingDependencyIsError.HasValue ? Item.MissingDependencyIsError.Value : true;
            if( m.MissingDependencyIsError != itemMissingDependencyIsError )
            {
                if( m.IgnoreMissingDependencyIsError )
                {
                    if( itemMissingDependencyIsError ) Engine.Logger.Trace( "SqlManager is configured to ignore MissingDependencyIsError." );
                }
                else
                {
                    m.MissingDependencyIsError = itemMissingDependencyIsError;
                    configRestorer = Util.CreateDisposableAction( () => m.MissingDependencyIsError = !m.MissingDependencyIsError );
                }
            }
            using( configRestorer )
            {
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
            }
            return true;
        }

        public static SqlManager FindManagerFromLocation( IActivityLogger logger, ISqlManagerProvider provider, string fullName )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( provider == null ) throw new ArgumentNullException( "provider" );
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            SqlManager m = null;
            string context, location, name;
            if( !DefaultContextLocNaming.TryParse( fullName, out context, out location, out name ) || String.IsNullOrEmpty( location ) )
            {
                logger.Error( "Unable to extract a location from FullName '{0}' in order to find a Sql connection.", fullName );
            }
            else if( (m = provider.FindManagerByName( location )) == null )
            {
                logger.Error( "Location '{0}' from FullName '{1}' can not be mapped to an existing Sql Connection.", location, fullName );
            }
            return m;
        }


    }
}
