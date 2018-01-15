using CK.Core;
using CK.SqlServer;
using System;
using System.Data.SqlClient;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo
{
    public class TestActorContext : IActorCallContext, IDisposable
    {
        readonly int _actorId;
        readonly SqlStandardCallContext _exec;

        public TestActorContext( int actorId )
        {
            _actorId = actorId;
            _exec = new SqlStandardCallContext();
        }

        public SqlConnection this[ISqlConnectionStringProvider p] => _exec[p];

        public SqlConnection this[string connectionString] => _exec[connectionString];

        int IActorCallContext.ActorId => _actorId; 

        ISqlCommandExecutor ISqlCallContext.Executor => _exec;

        public ISqlConnectionController GetConnectionController( string connectionString ) => _exec.GetConnectionController( connectionString );

        public IActivityMonitor Monitor => TestHelper.Monitor;

        void IDisposable.Dispose() => _exec.Dispose();


    }
}
