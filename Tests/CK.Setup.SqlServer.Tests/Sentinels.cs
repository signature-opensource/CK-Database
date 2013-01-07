using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using NUnit.Framework;

namespace CK.Setup.SqlServer.Tests
{
    public class SqlSetupCenterFactory
    {
        public static SqlSetupCenter Create( SqlSetupContext ctx, bool useSentinel = true )
        {
            if( useSentinel ) return new SentinelSqlSetupCenter( ctx );

            return new SqlSetupCenter( ctx );
        }
    }

    class SentinelSqlSetupCenter : SqlSetupCenter
    {
        readonly SqlSetupContext _ctx;

        public SentinelSqlSetupCenter( SqlSetupContext ctx )
            : base( ctx )
        {
            _ctx = ctx;
        }

        protected internal override Setup.SetupDriver CreateDriver( Type driverType, Setup.SetupDriver.BuildInfo info )
        {
            if( driverType == typeof( SqlDatabaseConnectionSetupDriver ) ) return new SentinelSqlDatabaseConnectionSetupDriver( info, _ctx );
            return base.CreateDriver( driverType, info );
        }
    }

    class SentinelSqlDatabaseConnectionSetupDriver : SqlDatabaseConnectionSetupDriver
    {
        public SentinelSqlDatabaseConnectionSetupDriver( BuildInfo info, ISqlManagerProvider sql )
            : base( info, sql )
        {
        }

        protected override bool Init()
        {
            bool baseResult = base.Init();
            Assert.That( baseResult && _connection != null );
            
            foreach( var name in Item.SqlDatabase.Schemas )
            {
                object res = _connection.Connection.ExecuteScalar( String.Format( "select count(*) from sys.schemas where name = '{0}';", name ) );
                Assert.That( (int)res == 1 );
            }

            return true;
        }
    }
}
