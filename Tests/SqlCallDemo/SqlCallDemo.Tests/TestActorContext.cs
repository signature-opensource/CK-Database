using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;
using CK.SqlServer.Setup;

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

        void IDisposable.Dispose() => _exec.Dispose();

    }
}
