using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{
    public class TestActorContext : SqlStandardCallContext, IActorCallContext
    {
        public TestActorContext( int actorId )
        {
            ActorId = actorId;
        }

        public int ActorId { get; set; }
    }
}
