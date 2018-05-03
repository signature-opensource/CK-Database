using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Setup item that models the Sql database.
    /// </summary>
    public class SqlDatabaseItem : StObjDynamicContainerItem
    {
        /// <summary>
        /// All <see cref="SqlDatabaseItem"/> share the same name: "SqlDatabase".
        /// Their full names are defined only by context and location (the logical databasename): 
        /// "[context]dbName^SqlDatabase".
        /// </summary>
        public static string SqlDatabaseItemName = "SqlDatabase";

        internal readonly SqlDatabaseConnectionItem ConnectionItem;

        class Model : ISetupItem, IDependentItemGroup, IDependentItemGroupRef
        {
            readonly SqlDatabaseItem _holder;

            public Model( SqlDatabaseItem h )
            {
                _holder = h;
            }

            public IDependentItemContainerRef Container => null;

            public string Context => _holder.Context;

            public string Location => _holder.Location;

            public string Name => "Model." + _holder.Name;

            public string FullName => DefaultContextLocNaming.Format( _holder.Context, _holder.Location, Name );

            string IContextLocNaming.TransformArg => null;

            public IDependentItemRef Generalization => null;

            public IEnumerable<IDependentItemGroupRef> Groups => null;

            public IEnumerable<IDependentItemRef> RequiredBy => null;

            public IEnumerable<IDependentItemRef> Requires => new[] { _holder.ConnectionItem };

            public string TransformArg => null;

            public bool Optional => false;

            public IEnumerable<IDependentItemRef> Children => null;

            public object StartDependencySort( IActivityMonitor m ) => null;
        }

        /// <summary>
        /// >Initializes a new <see cref="SqlDatabaseItem"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="data">The setup data from actual object.</param>
        public SqlDatabaseItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data, typeof(SqlDatabaseItemDriver) )
        {
            Context = data.StObj.Context.Context;
            Location = ActualObject.Name;
            Name = SqlDatabaseItemName;
            ConnectionItem = new SqlDatabaseConnectionItem( this );
            Requires.Add( new Model( this ) );
        }

        /// <summary>
        /// Masked to return a <see cref="SqlDatabase"/>.
        /// </summary>
        public new SqlDatabase ActualObject => (SqlDatabase)base.ActualObject;

        /// <summary>
        /// Gets the name of the SqlDatabaseItem based on the context and location.
        /// </summary>
        /// <param name="contextLocName">The non null context-locaton-name.</param>
        /// <returns>The associated database item name.</returns>
        public static string ItemNameFor( IContextLocNaming contextLocName )
        {
            return DefaultContextLocNaming.Format( contextLocName.Context, contextLocName.Location, SqlDatabaseItemName );
        }
    }
}
