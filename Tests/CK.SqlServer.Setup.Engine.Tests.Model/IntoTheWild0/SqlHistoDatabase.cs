#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\IntoTheWild0\SqlHistoDatabase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;

namespace CK.SqlServer.Setup
{
    [RemoveDefaultContext]
    [AddContext( "dbHisto" )]
    public class SqlHistoDatabase : SqlDatabase, IAmbientContract
    {
        public SqlHistoDatabase()
            : base( "dbHisto" )
        {
            InstallCore = true;
        }

        void Construct( string connectionString )
        {
            ConnectionString = connectionString;
        }
        
    }
}
