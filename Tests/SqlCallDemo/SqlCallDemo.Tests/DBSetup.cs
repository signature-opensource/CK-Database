using CK.Testing;
using NUnit.Framework;
using static CK.Testing.SqlServerTestHelper;


public class SharedEngineConfigurator
{
    [OneTimeSetUp]
    public void EnsureSqlServerConfigurationAspect() => TestHelper.SharedEngineSqlSupport();
}

namespace DBSetup
{
    [TestFixture]
    public class DBSetup : CK.DB.Tests.DBSetup
    {
    }
}
