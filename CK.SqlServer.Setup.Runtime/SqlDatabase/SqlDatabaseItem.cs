using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseItem : DynamicContainerItem
    {
        internal readonly SqlDatabaseConnectionItem ConnectionItem;
        
        public SqlDatabaseItem( IActivityLogger logger, IStObjSetupData data )
        {
            Object = (SqlDatabase)data.StObj.Object;
            Context = data.StObj.Context.Context;
            Location = Object.Name;
            ItemKind = (DependentItemKind)data.StObj.ItemKind;
            ConnectionItem = new SqlDatabaseConnectionItem( this );
            Requires.Add( ConnectionItem );
        }

        public SqlDatabase Object { get; private set; }

        protected override object StartDependencySort()
        {
            return typeof( SqlDatabaseSetupDriver );
        }
    }
}
