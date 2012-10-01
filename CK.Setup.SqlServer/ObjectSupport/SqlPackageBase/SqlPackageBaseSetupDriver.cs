using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlPackageBaseSetupDriver : SetupDriver
    {
        public SqlPackageBaseSetupDriver( BuildInfo info )
            : base( info ) 
        {
        }

        public new SqlPackageBaseItem Item { get { return (SqlPackageBaseItem)base.Item; } }

        protected override bool LoadScripts( ScriptCollector scripts )
        {
            var r = Item.Object.ResourceLocation;
            if( r != null )
            {
                IActivityLogger logger = Engine.Logger;
                if( r.Type == null )
                {
                    logger.Error( "ResourceLocator for '{0}' has no Type defined. A ResourceType must be set in order to load resources.", FullName );
                    return false;
                }
                else
                {
                    int nbScripts = scripts.AddFromResources( logger, "res-sql", r, FullName, ".sql" );
                    if( Item.Model != null ) nbScripts += scripts.AddFromResources( logger, "res-sql", r, "Model." + FullName, ".sql" );
                    
                    if( Item.Model == null )
                    {
                        logger.Trace( "{1} sql scripts in resource found for '{0}'.", FullName, nbScripts );
                    }
                    else
                    {
                        logger.Trace( "{1} sql scripts in resource found for '{0}' and 'Model.{0}'.", FullName, nbScripts );
                    }
                }
            }
            return true;
        }


    }
}
