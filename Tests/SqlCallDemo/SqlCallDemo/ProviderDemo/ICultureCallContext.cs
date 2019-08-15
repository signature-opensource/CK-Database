using CK.SqlServer;

namespace SqlCallDemo
{
    public interface ICultureCallContext : ISqlCallContext
    {
        int CultureId { get; }
    }

    public interface IDisposableCultureCallContext : ICultureCallContext, IDisposableSqlCallContext 
    {
    }
}
