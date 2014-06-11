using System;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlTableItem : SqlPackageBaseItem
    {
        public SqlTableItem( Func<SqlTable> package )
            : base( "ObjTable", typeof( SqlTableSetupDriver ), package )
        {
        }

        public SqlTableItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Name = data.FullNameWithoutContext;
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlTable"/>.
        /// </summary>
        public new SqlTable GetObject()
        { 
            return (SqlTable)base.GetObject(); 
        }

    }
}
