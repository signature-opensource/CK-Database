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
    public class SqlPackageBaseItemDriver : SetupItemDriver
    {
        readonly ISqlSetupAspect _aspects;
        readonly IReadOnlyList<SqlPackageScript>[] _scripts;
        SqlDatabaseItemDriver _dbDriver;

        public SqlPackageBaseItemDriver( BuildInfo info )
            : base( info ) 
        {
            SqlPackageBase p = Item.ActualObject;
            string schema = p.Schema;
            if( schema != null && p.Database != null ) p.Database.EnsureSchema( schema );
            Debug.Assert( (int)SetupCallGroupStep.Init == 1 && (int)SetupCallGroupStep.SettleContent == 6 );
            _aspects = info.Engine.GetSetupEngineAspect<ISqlSetupAspect>();
            _scripts = new IReadOnlyList<SqlPackageScript>[6];
        }

        /// <summary>
        /// Gets the database driver.
        /// </summary>
        public SqlDatabaseItemDriver DatabaseDriver => _dbDriver ?? (_dbDriver = (SqlDatabaseItemDriver)Engine.Drivers[Item.Groups.OfType<SqlDatabaseItem>().Single()]);

        public new SqlPackageBaseItem Item => (SqlPackageBaseItem)base.Item;

        protected override bool ExecutePreInit()
        {
            if( Item.ResourceLocation?.Type == null )
            {
                Engine.Monitor.Error().Send( "ResourceLocator for '{0}' has no Type defined. A ResourceType must be set in order to load resources.", FullName );
                return false;
            }
            var externalVersion = ExternalVersion?.Version;
            if( !CreateScriptHandlerFor( this, Item.ResourceLocation, ItemVersion, externalVersion ) ) return false;
            if( Item.Model != null && !CreateScriptHandlerFor( Engine.Drivers[Item.Model], Item.ResourceLocation, ItemVersion, externalVersion ) ) return false;
            if( Item.ObjectsPackage != null && !CreateScriptHandlerFor( Engine.Drivers[Item.ObjectsPackage], Item.ResourceLocation, ItemVersion, externalVersion ) ) return false;
            return true;
        }

        /// <summary>
        /// Loads the init/install/settle scripts, filters them thanks to the provided target version (the 
        /// current, latest, one) and the currently installed version (that is null if no previous version has been 
        /// installed yet). The selected scripts are then given to a <see cref="SetupHandler"/> that is registered
        /// on the driver object: this SetupHandler will <see cref="SqlDatabaseItemDriver.InstallScript(SqlPackageScript)"/> 
        /// the appropriate scripts into this <see cref="DatabaseDriver"/> for each <see cref="SetupCallGroupStep"/>.
        /// </summary>
        /// <param name="driver">The driver to which scripts must be associated.</param>
        /// <param name="resLoc">The resource locator to use.</param>
        /// <param name="target">The current version.</param>
        /// <param name="externalVersion">The existing version if any.</param>
        /// <returns>True on success, false otherwise.</returns>
        protected bool CreateScriptHandlerFor( SetupItemDriver driver, ResourceLocator resLoc, Version target, Version externalVersion = null )
        {
            ScriptsCollection c = LoadResourceScriptsFor( driver.Item, resLoc );
            bool externalLoadError = false;
            using( Engine.Monitor.OnError( () => externalLoadError = true ) )
            {
                if( !LoadExternalScriptsFor( Engine.Monitor, driver.Item, c ) )
                {
                    if( !externalLoadError ) Engine.Monitor.Error().Send( $"Error while loading external scripts for '{driver.Item.FullName}'." );
                    return false;
                }
            }
            if( c.Count > 0 )
            {
                bool hasScripts = false;
                var scripts = new IReadOnlyList<SqlPackageScript>[6];
                for( var step = SetupCallGroupStep.Init; step <= SetupCallGroupStep.SettleContent; ++step )
                {
                    scripts[(int)step - 1] = Util.Array.Empty<SqlPackageScript>();
                    ScriptVector v = c.GetScriptVector( step, ExternalVersion?.Version, ItemVersion );
                    if( v != null && v.Scripts.Count > 0 )
                    {
                        List<SqlPackageScript> collector = null;
                        foreach( ISetupScript s in v.Scripts.Select( cs => cs.Script ) )
                        {
                            if( !ProcessSetupScripts( s, ref collector ) ) return false;
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
        /// By default, this method does nothing and returns true.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="locName">The context-location-name to find scripts for.</param>
        /// <returns>True on success, false on error.</returns>
        protected virtual bool LoadExternalScriptsFor( IActivityMonitor monitor, IContextLocNaming locName, ScriptsCollection collector )
        {
            return true;
        }

        ScriptsCollection LoadResourceScriptsFor( IContextLocNaming locName, ResourceLocator resLoc )
        {
            string context, location, name;
            var scripts = new ScriptsCollection();
            context = locName.Context;
            location = locName.Location;
            name = locName.Name;
            var monitor = Engine.Monitor;
            int nbScripts = scripts.AddFromResources( monitor, "res-sql", resLoc, context, location, name, ".sql" );
            nbScripts += scripts.AddFromResources( monitor, "res-y4", resLoc, context, location, name, ".y4" );
            monitor.Info().Send( "{1} sql scripts in resource found for '{0}' in '{2}.", name, nbScripts, resLoc );
            return scripts;
        }

        bool ProcessSetupScripts( ISetupScript s, ref List<SqlPackageScript> collector )
        {
            string body = s.GetScript();
            if( s.ScriptSource.EndsWith( "-y4", StringComparison.Ordinal ) )
            {
                body = SqlPackageBaseItem.ProcessY4Template( Engine.Monitor, this, Item, Item.ActualObject, s.Name.FileName, body );
            }
            var tagHandler = new SimpleScriptTagHandler( body );
            if( !tagHandler.Expand( Engine.Monitor, true ) ) return false;
            int idx = 0;
            foreach( var one in tagHandler.SplitScript() )
            {
                string key = s.GetScriptKey( one.Label ?? "AutoLabel" + idx );
                var result = _aspects.SqlParser.Parse( one.Body );
                if( result.IsError )
                {
                    result.LogOnError( Engine.Monitor );
                    return false;
                }
                if( collector == null ) collector = new List<SqlPackageScript>();
                collector.Add( new SqlPackageScript( this, s.Name.CallContainerStep, key, result.Result ) );
                ++idx;
            }
            return true;
        }

        class ScriptHandler : SetupHandler
        {
            readonly SqlPackageBaseItemDriver _main;
            readonly IReadOnlyList<SqlPackageScript>[] _scripts;

            public ScriptHandler( SqlPackageBaseItemDriver main, SetupItemDriver d, IReadOnlyList<SqlPackageScript>[] scripts )
                : base( d )
            {
                _main = main;
                _scripts = scripts;
            }

            protected override bool OnStep( SetupCallGroupStep step )
            {
                foreach( var s in _scripts[(int)step - 1] )
                {
                    if( !_main.DatabaseDriver.InstallScript( s ) ) return false;
                }
                return true;
            }
        }

    }
}
