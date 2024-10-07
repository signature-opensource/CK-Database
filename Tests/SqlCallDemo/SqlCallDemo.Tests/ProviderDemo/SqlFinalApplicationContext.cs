using CK.SqlServer;

namespace SqlCallDemo.Tests.ProviderDemo;


/// <summary>
/// The application context here mixes all the required call contexts it needs to be able to call all the apis the application needs.
/// </summary>
public class SqlFinalApplicationContext : SqlTransactionCallContext, IDisposableAllCallContext
{
    public SqlFinalApplicationContext( int actorId, int xlcid, int zoneId )
    {
        ActorId = actorId;
        CultureId = xlcid;
        TenantId = zoneId;
    }

    public int ActorId { get; private set; }

    public int CultureId { get; private set; }

    public int TenantId { get; private set; }
}
