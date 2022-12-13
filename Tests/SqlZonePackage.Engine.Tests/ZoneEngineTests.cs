using CK.Core;
using CK.Setup;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CK.Testing.SqlServerTestHelper;

namespace SqlZonePackage.Engine.Tests
{
    [TestFixture]
    public class ZoneEngineTests
    {
        [Test]
        public void setup_whole_assembly()
        {
            var config = new StObjEngineConfiguration();
            config.Aspects.Add( new SetupableAspectConfiguration() );
            config.Aspects.Add( new SqlSetupAspectConfiguration() { DefaultDatabaseConnectionString = TestHelper.GetConnectionString() } );
            config.BinPaths.Add( new BinPathConfiguration() {
                CompileOption = CompileOption.Compile,
                Assemblies =
                {
                    "SqlZonePackage",
                    "CK.StObj.Model",
                    "CK.Setupable.Model",
                    "CK.SqlServer.Setup.Model",
                    "SqlActorPackage",
                    "CK.SqlServer"
                } } );
            var engine = new StObjEngine( TestHelper.Monitor, config );
            engine.Run().Should().BeTrue();
        }
    }
}
