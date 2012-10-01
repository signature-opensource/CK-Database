using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlTableItem : SqlPackageBaseItem
    {
        public SqlTableItem( SqlTable package )
            : base( "ObjTable", typeof( SqlTableSetupDriver ), package )
        {
        }

        public SqlTableItem( IActivityLogger logger, IStObjSetupData data )
            : base( logger, data )
        {
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlTable"/>.
        /// </summary>
        public new SqlTable Object
        { 
            get { return (SqlTable)base.Object; } 
        }

    }
}
