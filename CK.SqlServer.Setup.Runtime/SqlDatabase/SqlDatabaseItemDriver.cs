#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Setup;
using CK.SqlServer.Parser;
using System.Collections.Generic;
using System;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseItemDriver : SetupItemDriver
    {
        readonly SqlDatabaseConnectionItemDriver _connection;
        readonly List<ISqlServerObject> _sqlObjects;

        public SqlDatabaseItemDriver( BuildInfo info )
            : base( info )
        {
            _connection = (SqlDatabaseConnectionItemDriver)Engine.Drivers[Item.ConnectionItem];
            _sqlObjects = new List<ISqlServerObject>();
        }

        /// <summary>
        /// Masked Item to formally be associated to a <see cref="SqlDatabaseItem"/> item.
        /// </summary>
        public new SqlDatabaseItem Item => (SqlDatabaseItem)base.Item;

        /// <summary>
        /// Gets the Sql manager for this database.
        /// </summary>
        public ISqlManagerBase SqlManager => _connection.SqlManager;

    }
}
