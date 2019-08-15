#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlTable\SqlTableItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Sql table item.
    /// </summary>
    public class SqlTableItem : SqlPackageBaseItem
    {
        /// <summary>
        /// Initializes a new <see cref="SqlTableItem"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="data">The StObj data.</param>
        public SqlTableItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Name = data.FullNameWithoutContext;
        }

        /// <summary>
        /// Masked to formally be associated to <see cref="SqlTable"/>.
        /// </summary>
        public new SqlTable ActualObject => (SqlTable)base.ActualObject;

    }
}
