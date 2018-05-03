#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlTable\SqlTableSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Driver for <see cref="SqlTableItem"/>.
    /// </summary>
    public class SqlTableItemDriver : SqlPackageBaseItemDriver
    {
        /// <summary>
        /// Initializes a new <see cref="SqlTableItemDriver"/>.
        /// </summary>
        /// <param name="info">Driver build information (required by base SetupItemDriver).</param>
        public SqlTableItemDriver( BuildInfo info )
            : base( info ) 
        {
        }

        /// <summary>
        /// Masked to formally associates a <see cref="SqlTableItem"/> type.
        /// </summary>
        public new SqlTableItem Item => (SqlTableItem)base.Item;

    }
}
