using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using CK.Core;
using CK.SqlServer;

namespace SqlActorPackage.Basic
{

    [SqlPackage( Schema = "CK", Package = typeof( Package )/*, Database = typeof( SqlDefaultDatabase ), ResourcePath = "Res"*/ )]
    [Versions( "1.0.0" )]
    [Setup( DriverTypeName = "SqlActorPackage.Runtime.BuggyPackageDriver, SqlActorPackage.Runtime" )]
    public abstract class BuggyPackage : SqlPackage
    {
        void StObjConstruct( IActivityMonitor monitor )
        {
            monitor.Info( "BuggyPackage StObjConstruct called." );
        }

        void StObjInitialize( IActivityMonitor monitor, IStObjObjectMap map )
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
