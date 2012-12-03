using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.SqlServer;

namespace CK.Setup.SqlServer
{
    public class SqlDatabaseItem : DynamicContainerItem
    {
        internal readonly SqlDatabaseConnectionItem ConnectionItem;
        
        public SqlDatabaseItem()
        {
            Object = new SqlDatabase();
            ConnectionItem = new SqlDatabaseConnectionItem( this );
            Requires.Add( ConnectionItem );
        }

        public SqlDatabaseItem( IActivityLogger logger, IStObjSetupData data )
        {
            Object = (SqlDatabase)data.StObj.Object;
            Context = data.StObj.Context;
            Location = Object.Name;
            ItemKind = data.StObj.ItemKind;
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
