#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlPackageBase\SqlPackageBaseSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlPackageBaseItemDriver : SetupItemDriver
    {
        SqlDatabaseItemDriver _dbDriver;

        public SqlPackageBaseItemDriver( BuildInfo info )
            : base( info ) 
        {
            SqlPackageBase p = Item.ActualObject;
            string schema = p.Schema;
            if( schema != null && p.Database != null ) p.Database.EnsureSchema( schema ); 
        }

        /// <summary>
        /// Gets the database driver.
        /// </summary>
        public SqlDatabaseItemDriver DatabaseDriver => _dbDriver ?? (_dbDriver = (SqlDatabaseItemDriver)Engine.Drivers[Item.Groups.OfType<SqlDatabaseItem>().Single()]);

        public new SqlPackageBaseItem Item => (SqlPackageBaseItem)base.Item;

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
                    string context, location, name, targetName;
                    if( !DefaultContextLocNaming.TryParse( FullName, out context, out location, out name, out targetName ) )
                    {
                        monitor.Error().Send( "Unable to parse '{0}' to extract context and location.", FullName );
                        return false;
                    }
                    int nbScripts = scripts.AddFromResources( monitor, "res-sql", r, context, location, name, ".sql" );
                    if( Item.Model != null ) nbScripts += scripts.AddFromResources( monitor, "res-sql", r, context, location, "Model." + name, ".sql" );
                    if( Item.ObjectsPackage != null ) nbScripts += scripts.AddFromResources( monitor, "res-sql", r, context, location, "Objects." + name, ".sql" );

                    nbScripts = scripts.AddFromResources( monitor, "res-y4", r, context, location, name, ".y4" );
                    if( Item.Model != null ) nbScripts += scripts.AddFromResources( monitor, "res-y4", r, context, location, "Model." + name, ".y4" );
                    if( Item.ObjectsPackage != null ) nbScripts += scripts.AddFromResources( monitor, "res-y4", r, context, location, "Objects." + name, ".y4" );

                    if( Item.Model != null )
                    {
                        if( Item.ObjectsPackage != null )
                        {
                            monitor.Trace().Send( "{1} sql scripts in resource found for '{0}' and 'Model.{0}' and 'Objects.{0}' in '{2}'.", name, nbScripts, r );
                        }
                        else monitor.Trace().Send( "{1} sql scripts in resource found for '{0}' and 'Model.{0}' in '{2}'.", name, nbScripts, r );
                    }
                    else if( Item.ObjectsPackage != null )
                    {
                        monitor.Trace().Send( "{1} sql scripts in resource found for '{0}' and 'Objects.{0}' in '{2}'.", name, nbScripts, r );
                    }
                    else
                    {
                        monitor.Trace().Send( "{1} sql scripts in resource found for '{0}' in '{2}.", name, nbScripts, r );
                    }
                }
            }
            return true;
        }


    }
}
