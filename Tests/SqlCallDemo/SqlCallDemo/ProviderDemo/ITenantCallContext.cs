using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{
    public interface ITenantCallContext : ISqlCallContext
    {
        int TenantId { get; }
    }

    public interface IDisposableTenantCallContext : ITenantCallContext, IDisposableSqlCallContext 
    {
    }
}
