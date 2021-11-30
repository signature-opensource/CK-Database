#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\SqlObjectSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Linq;
using CK.Core;
using CK.Setup;
using System.Text;
using System.Diagnostics;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Driver for <see cref="SqlObjectItem"/>.
    /// </summary>
    public class SqlObjectItemDriver : SetupItemDriver
    {
        SqlDatabaseItemDriver _dbDriver;

        /// <summary>
        /// Initializes a new <see cref="SqlObjectItemDriver"/>.
        /// </summary>
        /// <param name="info">Driver build information (required by base SetupItemDriver).</param>
        public SqlObjectItemDriver( BuildInfo info )
            : base( info )
        {
        }

        /// <summary>
        /// Masked to formally associates a <see cref="SqlObjectItem"/> type.
        /// </summary>
        public new SqlObjectItem Item => (SqlObjectItem)base.Item;

        /// <summary>
        /// Gets the database driver.
        /// </summary>
        public SqlDatabaseItemDriver DatabaseDriver => _dbDriver ?? (_dbDriver = Drivers.Find<SqlDatabaseItemDriver>( SqlDatabaseItem.ItemNameFor(Item) ));

        /// <summary>
        /// Installs the object.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="beforeHandlers">True when called before setup handlers.</param>
        /// <returns>True on success, false on error.</returns>
        protected override bool Install( IActivityMonitor monitor, bool beforeHandlers )
        {
            if( beforeHandlers ) return true;
            return LegacyInstall( monitor );
        }

        internal bool OnTargetTransformed( IActivityMonitor monitor, SqlTransformerItemDriver transformerDriver, ISqlServerObject transformed )
        {
            Debug.Assert( Item.TransformTarget != null, "We are a transformation source." );
            // Updates the target with the current transformed object.
            Item.TransformTarget.SqlObject = transformed;
            // Systematically updates the object.
            StringBuilder b = new StringBuilder();
            Item.TransformTarget.WriteCreate( b, alreadyExists: true );
            if( transformerDriver.Item.IsLastTransformer )
            {
                b.AppendLine()
                    .Append( $"-- This has been transformed by " )
                    .AppendStrings( Item.Transformers.Select( t => (t.TransformTarget ?? t).FullName ) )
                    .AppendLine();
            }
            return ExpandAndExecuteScript( monitor, b.ToString() );
        }

        bool LegacyInstall( IActivityMonitor monitor )
        {
            Debug.Assert( Item.TransformTarget == null || Item.TransformSource == null, "Both can not be set on the same item." );

            // A target item is not installed: its last transformer did the job.
            if( Item.TransformSource != null ) return true;

            StringBuilder b = new StringBuilder();
            // If this is the first occurrence of a transformed item OR the only one (no transformation).
            Item.WriteSafeDrop( b );
            if( !DatabaseDriver.SqlManager.ExecuteOneScript( b.ToString(), monitor ) ) return false;
            b.Clear();
            if( Item.TransformTarget != null )
            {
                b.Append( $"-- This will be transformed by " )
                    .AppendStrings( Item.Transformers.Select( t => (t.TransformTarget ?? t).FullName ) )
                    .AppendLine();
            }
            Item.WriteCreate( b, alreadyExists: false );
            return ExpandAndExecuteScript( monitor, b.ToString() );
        }

        bool ExpandAndExecuteScript( IActivityMonitor monitor, string s )
        {
            var tagHandler = new SimpleScriptTagHandler( s );
            if( !tagHandler.Expand( monitor, true ) ) return false;
            var scripts = tagHandler.SplitScript();
            if( !DatabaseDriver.SqlManager.ExecuteScripts( scripts.Select( c => c.Body ), monitor ) )
            {
                return false;
            }
            return true;
        }
    }
}
