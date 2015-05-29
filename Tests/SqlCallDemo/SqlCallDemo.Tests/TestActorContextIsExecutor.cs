using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;

namespace SqlCallDemo
{
    public class TestActorContextIsExecutor : SqlStandardCallContext, IActorCallContextIsExecutor
    {
        public TestActorContextIsExecutor( int actorId )
        {
            ActorId = actorId;
        }

        public int ActorId { get; set; }
    }
}
