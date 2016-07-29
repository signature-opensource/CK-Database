#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseSetupDriver : SetupItemDriver
    {
        readonly SqlDatabaseConnectionSetupDriver _connection;

        public SqlDatabaseSetupDriver( BuildInfo info )
            : base( info )
        {
            _connection = (SqlDatabaseConnectionSetupDriver)Engine.AllDrivers[Item.ConnectionItem];
        }

        /// <summary>
        /// Masked Item to formally be associated to a <see cref="SqlDatabaseItem"/> item.
        /// </summary>
        public new SqlDatabaseItem Item => (SqlDatabaseItem)base.Item;

    }
}
