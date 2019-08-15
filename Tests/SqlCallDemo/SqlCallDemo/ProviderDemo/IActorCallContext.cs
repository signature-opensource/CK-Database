using CK.SqlServer;

namespace SqlCallDemo
{
    /// <summary>
    /// A context used by methods does not need to be IDisposable.
    /// However, in order to support providers, for each context a IDisposable version must be defined. See below.
    /// </summary>
    public interface IActorCallContext : ISqlCallContext
    {
        int ActorId { get; }
    }

    /// <summary>
    /// The disposable version that is used by context implementations.
    /// Thanks to the covariance, this enables a provider of mixed contexts to 
    /// transparentely behave as a provider for each of the component (the sub contexts).
    /// For simple context, the disposable version must compose the base IDisposableSqlCallContext and the non-disposable version.
    /// Actually this is the rule to follow for all the disposable context types. See <see cref="IActorCultureCallContext"/>.
    /// </summary>
    public interface IDisposableActorCallContext : IActorCallContext, IDisposableSqlCallContext
    {
    }


}
