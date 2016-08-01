#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\SqlObjectSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.IO;
using System.Linq;
using CK.Core;
using CK.Setup;
using System.Text;
using CK.Text;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Driver for <see cref="SqlObjectItem"/>.
    /// </summary>
    public class SqlObjectItemDriver : SetupItemDriver
    {
        readonly ISqlSetupAspect _aspects;
        SqlDatabaseItemDriver _dbDriver;

        public SqlObjectItemDriver( BuildInfo info )
            : base( info )
        {
            _aspects = info.Engine.GetSetupEngineAspect<ISqlSetupAspect>();
        }

        public new SqlObjectItem Item => (SqlObjectItem)base.Item;

        /// <summary>
        /// Gets the database driver.
        /// </summary>
        public SqlDatabaseItemDriver DatabaseDriver => _dbDriver ?? (_dbDriver = (SqlDatabaseItemDriver)Engine.Drivers[SqlDatabaseItem.ItemNameFor(Item)]);

        protected override bool Install( bool beforeHandlers )
        {
            if( beforeHandlers ) return true;

            if( ExternalVersion != null && ExternalVersion.Version == Item.Version ) return true;

            if( !_aspects.GlobalResolution ) return LegacyInstall();

            Debug.Assert( Item.TransformTarget == null || Item.TransformSource == null, "Both can not be set on the same item." );

            return LegacyInstall();

        }

        bool LegacyInstall()
        {
            ISqlManagerBase m = DatabaseDriver.SqlManager;

            string s;
            StringBuilder b = new StringBuilder();

            IDisposable configRestorer = null;
            bool itemMissingDependencyIsError = Item.MissingDependencyIsError.HasValue ? Item.MissingDependencyIsError.Value : true;
            if( m.MissingDependencyIsError != itemMissingDependencyIsError )
            {
                if( m.IgnoreMissingDependencyIsError )
                {
                    if( itemMissingDependencyIsError ) Engine.Monitor.Trace().Send( "SqlManager is configured to ignore MissingDependencyIsError." );
                }
                else
                {
                    m.MissingDependencyIsError = itemMissingDependencyIsError;
                    configRestorer = Util.CreateDisposableAction( () => m.MissingDependencyIsError = !m.MissingDependencyIsError );
                }
            }
            using( configRestorer )
            {
                Item.WriteDrop( b );
                Debug.Assert( Item.TransformTarget == null || Item.TransformSource == null, "Both can not be set on the same item." );
                if( Item.TransformTarget != null )
                {
                    b.Append( $"-- This will be transformed by " )
                        .AppendStrings( Item.Transformers.Select( t => (t.TransformTarget ?? t).FullName ) )
                        .AppendLine();
                }
                else if( Item.TransformSource != null )
                {
                    b.Append( $"-- This has been transformed by " )
                        .AppendStrings( Item.TransformSource.Transformers.Select( t => (t.TransformTarget ?? t).FullName ) )
                        .AppendLine();
                }
                s = b.ToString();
                if( !m.ExecuteOneScript( s, Engine.Monitor ) ) return false;
                b.Clear();

                Item.WriteCreate( b );
                s = b.ToString();

                var tagHandler = new SimpleScriptTagHandler( s );
                if( !tagHandler.Expand( Engine.Monitor, true ) ) return false;
                var scripts = tagHandler.SplitScript();
                if( !m.ExecuteScripts( scripts.Select( c => c.Body ), Engine.Monitor ) ) return false;
            }
            return true;
        }
    }
}
