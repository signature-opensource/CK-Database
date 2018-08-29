#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\KindOfActorPackage\Basic\Package.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SqlActorPackage.Basic
{

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourcePath = "Res" ), Versions( "1.0.0" )]
    [Setup( DriverTypeName = "SqlActorPackage.Runtime.BuggyPackageDriver, SqlActorPackage.Runtime" )]
    public abstract class BuggyPackage : SqlPackage
    {
        void StObjConstruct( IActivityMonitor monitor )
        {
            monitor.Info( "BuggyPackage StObjConstruct called." );
        }

        void StObjInitialize( IActivityMonitor monitor, IStObjMap map )
        {
            monitor.Info( "BuggyPackage StObjInitialize called." );
        }

        public List<(int Id, DateTime SetupTime)> ReadSettleContentInfo( ISqlCallContext ctx ) => ReadSettleContentInfo( ctx[Database] );

        public static List<(int Id, DateTime SetupTime)> ReadSettleContentInfo( ISqlConnectionController c )
        {
            using( var cmd = new SqlCommand( "if object_id('CK.tBuggyPackageSettleContent') is not null select Id, SetupTime from CK.tBuggyPackageSettleContent order by SetupTime desc" ) )
            {
                return c.ExecuteReader( cmd, r => (r.GetInt32( 0 ), r.GetDateTime( 1 )) );
            }
        }
    }
}
