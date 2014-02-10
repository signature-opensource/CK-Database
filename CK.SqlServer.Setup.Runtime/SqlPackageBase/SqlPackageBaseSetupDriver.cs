using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlPackageBaseSetupDriver : SetupDriver
    {
        public SqlPackageBaseSetupDriver( BuildInfo info )
            : base( info ) 
        {
        }

        public new SqlPackageBaseItem Item { get { return (SqlPackageBaseItem)base.Item; } }

        protected override bool LoadScripts( IScriptCollector scripts )
        {
            var r = Item.ResourceLocation;
            if( r != null )
            {
                IActivityMonitor monitor = Engine.Monitor;
                if( r.Type == null )
                {
                    monitor.Error().Send( "ResourceLocator for '{0}' has no Type defined. A ResourceType must be set in order to load resources.", FullName );
                    return false;
                }
                else
                {
                    string context, location, name;
                    if( !DefaultContextLocNaming.TryParse( FullName, out context, out location, out name ) )
                    {
                        monitor.Error().Send( "Unable to parse '{0}' to extract context and location.", FullName );
                        return false;
                    }
                    else
                    {
                        int nbScripts = scripts.AddFromResources( monitor, "res-sql", r, context, location, name, ".sql" );
                        if( Item.Model != null ) nbScripts += scripts.AddFromResources( monitor, "res-sql", r, context, location, "Model." + name, ".sql" );

                        if( Item.Model == null )
                        {
                            monitor.Trace().Send( "{1} sql scripts in resource found for '{0}' in '{2}.", name, nbScripts, r );
                        }
                        else
                        {
                            monitor.Trace().Send( "{1} sql scripts in resource found for '{0}' and 'Model.{0}' in '{2}'.", name, nbScripts, r );
                        }
                    }
                }
            }
            return true;
        }


    }
}
