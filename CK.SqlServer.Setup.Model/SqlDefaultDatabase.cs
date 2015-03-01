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
    public class SqlDefaultDatabase : SqlDatabase, IAmbientContract
    {
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
