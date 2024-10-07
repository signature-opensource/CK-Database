using CK.SqlServer;

namespace SqlCallDemo;

public interface ITenantCallContext : ISqlCallContext
{
    int TenantId { get; }
}

public interface IDisposableTenantCallContext : ITenantCallContext, IDisposableSqlCallContext
{
}
