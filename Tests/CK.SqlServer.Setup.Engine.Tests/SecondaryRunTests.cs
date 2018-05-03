using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CK.Testing.DBSetupTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class SecondaryRunTests
    {
        [Test]
        public void SqlZonePackage_with_SqlActorPackage_SetupFolder()
        {
            var actorGeneratedPath = Path.Combine( TestHelper.BinFolder, "../ForActorOnly" );
            Directory.CreateDirectory( actorGeneratedPath );
            var config = XElement.Parse( $@"
            <C>
                <GeneratedAssemblyName>SqlActorPackage_SetupFolder</GeneratedAssemblyName>
                <Assemblies>
                    <Assembly>SqlActorPackage</Assembly>
                    <Assembly>SqlZonePackage</Assembly>
                </Assemblies>
                <SetupFolder>
                    <Directory>{actorGeneratedPath}</Directory>
                    <Assemblies>
                        <Assembly>SqlActorPackage</Assembly>
                    </Assemblies>
                </SetupFolder>
                <Aspect Type=""CK.Setup.SetupableAspectConfiguration, CK.Setupable.Model"" >
                </Aspect>
                <Aspect Type=""CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model"" >
                    <DefaultDatabaseConnectionString>{TestHelper.GetConnectionString()}</DefaultDatabaseConnectionString>
                </Aspect>
            </C>" );

            TestHelper.WithWeakAssemblyResolver( () => new StObjEngine( TestHelper.Monitor, config ).Run() )
                .Should().BeTrue();
            string generatedFile = Path.Combine( actorGeneratedPath, "SqlActorPackage_SetupFolder.dll" );
            File.Exists( generatedFile ).Should().BeTrue();
        }
    }
}
