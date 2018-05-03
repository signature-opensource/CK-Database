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

        public ISqlConnectionController this[ISqlConnectionStringProvider p] => _exec[p];

        public ISqlConnectionController this[string connectionString] => _exec[connectionString];

        int IActorCallContext.ActorId => _actorId; 

        ISqlCommandExecutor ISqlCallContext.Executor => _exec;

        public IActivityMonitor Monitor => TestHelper.Monitor;

        void IDisposable.Dispose() => _exec.Dispose();


    }
}
