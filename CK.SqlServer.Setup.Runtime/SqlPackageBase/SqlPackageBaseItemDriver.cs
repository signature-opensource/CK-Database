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
            int nbTotalScripts = 0;
            ScriptsCollection scripts;
            if( !LoadResourceScripts( Engine.Monitor, out scripts ) ) return false;
            for( var step = SetupCallGroupStep.Init; step <= SetupCallGroupStep.SettleContent; ++step )
            {
                _scripts[(int)step - 1] = Util.Array.Empty<SqlPackageScript>();
                ScriptVector v = scripts.GetScriptVector( step, ExternalVersion?.Version, ItemVersion );
                if( v != null && v.Scripts.Count > 0 )
                {
                    List<SqlPackageScript> collector = null;
                    foreach( ISetupScript s in v.Scripts.Select( cs => cs.Script ) )
                    {
                        if( !ProcessSetupScripts( s, ref collector ) ) return false;
                    }
                    if( collector != null )
                    {
                        _scripts[(int)step - 1] = collector;
                        nbTotalScripts += collector.Count;
                    }
                }
            }
            if( nbTotalScripts > 0 )
            {
                Engine.Monitor.Info().Send( $"{nbTotalScripts} sql scripts must run for '{FullName}': {_scripts[0].Count} Init, {_scripts[1].Count} InitContent, {_scripts[2].Count} Install, {_scripts[3].Count} InstallContent, {_scripts[4].Count} Settle, {_scripts[5].Count} SettleContent." );
            }
            return true;
        }

        bool LoadResourceScripts( IActivityMonitor monitor, out ScriptsCollection scripts )
        {
            scripts = null;
            var r = Item.ResourceLocation;
            if( r != null )
            {
                if( r.Type == null )
                {
                    monitor.Error().Send( "ResourceLocator for '{0}' has no Type defined. A ResourceType must be set in order to load resources.", FullName );
                    return false;
                }
                else
                {
                    string context, location, name;
#if DEBUG
                    string targetName;
                    Debug.Assert( DefaultContextLocNaming.TryParse( FullName, out context, out location, out name, out targetName ) );
                    Debug.Assert( context == Item.Context );
                    Debug.Assert( location == Item.Location );
                    Debug.Assert( name == Item.Name );
                    Debug.Assert( targetName == null );
#endif
                    scripts = new ScriptsCollection();
                    context = Item.Context;
                    location = Item.Location;
                    name = Item.Name;
                    int nbScripts = scripts.AddFromResources( monitor, "res-sql", r, context, location, name, ".sql" );
                    if( Item.Model != null ) nbScripts += scripts.AddFromResources( monitor, "res-sql", r, context, location, "Model." + name, ".sql" );
                    if( Item.ObjectsPackage != null ) nbScripts += scripts.AddFromResources( monitor, "res-sql", r, context, location, "Objects." + name, ".sql" );

                    nbScripts += scripts.AddFromResources( monitor, "res-y4", r, context, location, name, ".y4" );
                    if( Item.Model != null ) nbScripts += scripts.AddFromResources( monitor, "res-y4", r, context, location, "Model." + name, ".y4" );
                    if( Item.ObjectsPackage != null ) nbScripts += scripts.AddFromResources( monitor, "res-y4", r, context, location, "Objects." + name, ".y4" );

                    if( Item.Model != null )
                    {
                        if( Item.ObjectsPackage != null )
                        {
                            monitor.Info().Send( "{1} sql scripts in resource found for '{0}' and 'Model.{0}' and 'Objects.{0}' in '{2}'.", name, nbScripts, r );
                        }
                        else monitor.Info().Send( "{1} sql scripts in resource found for '{0}' and 'Model.{0}' in '{2}'.", name, nbScripts, r );
                    }
                    else if( Item.ObjectsPackage != null )
                    {
                        monitor.Info().Send( "{1} sql scripts in resource found for '{0}' and 'Objects.{0}' in '{2}'.", name, nbScripts, r );
                    }
                    else
                    {
                        monitor.Info().Send( "{1} sql scripts in resource found for '{0}' in '{2}.", name, nbScripts, r );
                    }
                }
            }
            return true;
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

        protected override bool OnStep( SetupCallGroupStep step, bool beforeHandlers )
        {
            if( !beforeHandlers )
            {
                foreach( var s in _scripts[(int)step - 1] )
                {
                    if( !DatabaseDriver.InstallScript( s ) ) return false;
                }
            }
            return true;
        }

    }
}
