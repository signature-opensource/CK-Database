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
    public class SqlTableItemDriver : SqlPackageBaseItemDriver
    {
        public SqlTableItemDriver( BuildInfo info )
            : base( info ) 
        {
        }

        public new SqlTableItem Item => (SqlTableItem)base.Item;

    }
}
