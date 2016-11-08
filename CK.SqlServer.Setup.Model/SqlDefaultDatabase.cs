#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\SqlDefaultDatabase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Typed <see cref="SqlDatabase"/> for the default <see cref="SqlDatabase"/>.
    /// </summary>
    public class SqlDefaultDatabase : SqlDatabase, IAmbientContract
    {
        /// <summary>
        /// Initializes the default database. Its name is <see cref="SqlDatabase.DefaultDatabaseName"/>
        /// and <see cref="SqlDatabase.DefaultSchemaName"/> is registered.
        /// </summary>
        public SqlDefaultDatabase()
            : base( DefaultDatabaseName )
        {
            EnsureSchema( DefaultSchemaName );
        }

        void Construct( string connectionString )
        {
            ConnectionString = connectionString;
        }
    }
}
