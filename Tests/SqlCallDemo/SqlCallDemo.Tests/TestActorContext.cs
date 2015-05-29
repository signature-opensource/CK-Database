using System;
using System.Collections.Generic;
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

        int IActorCallContext.ActorId
        {
            get { return _actorId; }
        }

        ISqlCommandExecutor ISqlCallContext.Executor
        {
            get { return _exec; }
        }

        void IDisposable.Dispose()
        {
            _exec.Dispose();
        }
    }
}
