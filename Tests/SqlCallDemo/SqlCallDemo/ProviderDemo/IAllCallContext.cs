namespace SqlCallDemo;

public interface IAllCallContext : IActorCallContext, ICultureCallContext, ITenantCallContext, IActorCultureCallContext
{
}

public interface IDisposableAllCallContext : IAllCallContext, IDisposableActorCallContext, IDisposableCultureCallContext, IDisposableTenantCallContext, IDisposableActorCultureCallContext
{
}
