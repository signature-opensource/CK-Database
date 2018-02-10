using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using System.Diagnostics;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Driver for <see cref="SqlPackageBaseItem"/>.
    /// </summary>
    public class SqlPackageBaseItemDriver : SetupItemDriver
    {
        SqlDatabaseItemDriver _dbDriver;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        public SqlPackageBaseItemDriver( BuildInfo info )
            : base( info ) 
        {
            SqlPackageBase p = Item.ActualObject;
            string schema = p.Schema;
            if( schema != null && p.Database != null ) p.Database.EnsureSchema( schema );
            Debug.Assert( (int)SetupCallGroupStep.Init == 1 && (int)SetupCallGroupStep.SettleContent == 6 );
        }

        /// <summary>
        /// Gets the database driver.
        /// </summary>
        public SqlDatabaseItemDriver DatabaseDriver => _dbDriver ?? (_dbDriver = Drivers.Find<SqlDatabaseItemDriver>( Item.Groups.OfType<SqlDatabaseItem>().Single() ) );

        /// <summary>
        /// Masked to be formally associated to <see cref="SqlPackageBaseItem"/> item.
        /// </summary>
        public new SqlPackageBaseItem Item => (SqlPackageBaseItem)base.Item;

        /// <summary>
        /// Loads the scripts from resources: found scripts are registered in a <see cref="SetupHandler"/>
        /// that will install the scripts.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false otherwise.</returns>
        protected override bool ExecutePreInit( IActivityMonitor monitor )
        {
            if( Item.ResourceLocation?.Type == null )
            {
                monitor.Error( $"ResourceLocator for '{FullName}' has no Type defined. A ResourceType must be set in order to load resources." );
                return false;
            }

            var externalVersion = ExternalVersion?.Version;
            if( !CreateScriptHandlerFor( monitor, this, Item.ResourceLocation, ItemVersion, externalVersion ) ) return false;
            if( Item.Model != null && !CreateScriptHandlerFor( monitor, Drivers[Item.Model], Item.ResourceLocation, ItemVersion, externalVersion ) ) return false;
            if( Item.ObjectsPackage != null && !CreateScriptHandlerFor( monitor, Drivers[Item.ObjectsPackage], Item.ResourceLocation, ItemVersion, externalVersion ) ) return false;
            return true;
        }

        /// <summary>
        /// Loads the init/install/settle scripts, filters them thanks to the provided target version (the 
        /// current, latest, one) and the currently installed version (that is null if no previous version has been 
        /// installed yet). The selected scripts are then given to a <see cref="SetupHandler"/> that is registered
        /// on the driver object: this SetupHandler will <see cref="SqlDatabaseItemDriver.InstallScript"/> 
        /// the appropriate scripts into this <see cref="DatabaseDriver"/> for each <see cref="SetupCallGroupStep"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="driver">The driver to which scripts must be associated.</param>
        /// <param name="resLoc">The resource locator to use.</param>
        /// <param name="target">The current version.</param>
        /// <param name="externalVersion">The existing version if any.</param>
        /// <returns>True on success, false otherwise.</returns>
        protected bool CreateScriptHandlerFor( IActivityMonitor monitor, SetupItemDriver driver, ResourceLocator resLoc, Version target, Version externalVersion = null )
        {
            ScriptsCollection c = LoadResourceScriptsFor( monitor, driver.Item, resLoc );
            bool externalLoadError = false;
            using( monitor.OnError( () => externalLoadError = true ) )
            {
                if( !LoadExternalScriptsFor( monitor, driver.Item, c ) )
                {
                    if( !externalLoadError ) monitor.Error( $"Error while loading external scripts for '{driver.Item.FullName}'." );
                    return false;
                }
            }
            if( c.Count > 0 )
            {
                bool hasScripts = false;
                var scripts = new IReadOnlyList<ISetupScript>[6];
                for( var step = SetupCallGroupStep.Init; step <= SetupCallGroupStep.SettleContent; ++step )
                {
                    scripts[(int)step - 1] = Util.Array.Empty<ISetupScript>();
                    ScriptVector v = c.GetScriptVector( step, ExternalVersion?.Version, ItemVersion );
                    if( v != null && v.Scripts.Count > 0 )
                    {
                        List<ISetupScript> collector = null;
                        foreach( ISetupScript s in v.Scripts.Select( cs => cs.Script ) )
                        {
                            ISetupScript sToAdd = s;
                            string body = s.GetScript();
                            if( s.Name.Extension == "y4" )
                            {
                                body = SqlPackageBaseItem.ProcessY4Template( monitor, this, Item, Item.ActualObject, s.Name.FileName, body );
                                if( body == null ) return false;
                                sToAdd = new SetupScript( s.Name, body );
                            }
                            if( collector == null ) collector = new List<ISetupScript>();
                            collector.Add( sToAdd );
                        }
                        if( collector != null )
                        {
                            scripts[(int)step - 1] = collector;
                            hasScripts = true;
                        }
                    }
                }
                if( hasScripts ) new ScriptHandler( this, driver, scripts );
            }
            return true;
        }

        /// <summary>
        /// Extension point that enables scripts to be found from file system or other locations.
        /// Scripts from resources are already loaded in the <param name="collector"/>.
        /// Following code adds an init script with no version (always executed):
        /// <code>
        /// collector.Add( monitor, SourceCodeSetupScript.CreateFromSourceCode( locName, "-- Hello!", "sql", SetupCallGroupStep.Init ) );
        /// </code>
        /// By default, this method does nothing and returns true.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="locName">The context-location-name to find scripts for.</param>
        /// <returns>True on success, false on error.</returns>
        protected virtual bool LoadExternalScriptsFor( IActivityMonitor monitor, IContextLocNaming locName, ScriptsCollection collector )
        {
            return true;
        }

        ScriptsCollection LoadResourceScriptsFor( IActivityMonitor monitor, IContextLocNaming locName, ResourceLocator resLoc )
        {
            string context, location, name;
            var scripts = new ScriptsCollection();
            context = locName.Context;
            location = locName.Location;
            name = locName.Name;
            int nbScripts = scripts.AddFromResources( monitor, resLoc, context, location, name, ".sql" );
            nbScripts += scripts.AddFromResources( monitor, resLoc, context, location, name, ".y4" );
            monitor.Info( $"{nbScripts} sql scripts in resource found for '{name}' in '{resLoc}." );
            return scripts;
        }

        class ScriptHandler : SetupHandler
        {
            readonly SqlPackageBaseItemDriver _main;
            readonly IReadOnlyList<ISetupScript>[] _scripts;

            public ScriptHandler( SqlPackageBaseItemDriver main, SetupItemDriver d, IReadOnlyList<ISetupScript>[] scripts )
                : base( d )
            {
                _main = main;
                _scripts = scripts;
            }

            protected override bool OnStep( IActivityMonitor monitor, SetupCallGroupStep step )
            {
                foreach( var s in _scripts[(int)step - 1] )
                {
                    if( !_main.DatabaseDriver.InstallScript( monitor, s ) ) return false;
                }
                return true;
            }
        }

    }
}
