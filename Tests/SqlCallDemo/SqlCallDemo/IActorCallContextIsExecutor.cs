using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{
    public interface IActorCallContextIsExecutor : ISqlCommandExecutor
    {
        int ActorId { get; }
    }
}
