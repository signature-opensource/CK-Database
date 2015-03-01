#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlTable\SqlTableItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
