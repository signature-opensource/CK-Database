namespace SqlCallDemo
{
    public interface ICultureTenantCallContext : ICultureCallContext, ITenantCallContext
    {
    }

    public interface IDisposableCultureTenantCallContext : ICultureTenantCallContext, IDisposableCultureCallContext, IDisposableTenantCallContext
    {
    }
}
