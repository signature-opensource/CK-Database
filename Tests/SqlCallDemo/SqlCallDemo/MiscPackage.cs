using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class MiscPackage : SqlPackage
    {
        [SqlProcedure( "sSleepProc" )]
        public abstract void CanWaitForTheDefaultCommandTimeout( SqlStandardCallContext ctx, int sleepTime );

        [SqlProcedure( "sSleepProc", TimeoutSeconds = 1 )]
        public abstract void CanWaitOnlyForOneSecond( SqlStandardCallContext ctx, int sleepTime );

    }
}
