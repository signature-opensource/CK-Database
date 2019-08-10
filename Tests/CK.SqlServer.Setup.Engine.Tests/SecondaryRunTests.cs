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
        public void SqlZonePackage_with_SqlActorPackage_BinPath()
        {
            var actorGeneratedPath = Path.Combine( TestHelper.BinFolder, "../ForActorOnly" );
            Directory.CreateDirectory( actorGeneratedPath );
            var config = XElement.Parse( $@"
            <C>
                <GeneratedAssemblyName>SqlActorPackage_BinPath</GeneratedAssemblyName>
                <BinPaths>
                    <BinPath>
                        <Path>{TestHelper.BinFolder}</Path>
                        <Assemblies>
                            <Assembly>SqlActorPackage</Assembly>
                            <Assembly>SqlZonePackage</Assembly>
                        </Assemblies>
                    </BinPath>
                    <BinPath>
                        <Path>{actorGeneratedPath}</Path>
                        <Assemblies>
                            <Assembly>SqlActorPackage</Assembly>
                        </Assemblies>
                    </BinPath>
                </BinPaths>
                <Aspect Type=""CK.Setup.SetupableAspectConfiguration, CK.Setupable.Model"" >
                </Aspect>
                <Aspect Type=""CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model"" >
                    <DefaultDatabaseConnectionString>{TestHelper.GetConnectionString()}</DefaultDatabaseConnectionString>
                </Aspect>
            </C>" );

            TestHelper.WithWeakAssemblyResolver( () => new StObjEngine( TestHelper.Monitor, config ).Run() )
                .Should().BeTrue();
            string generatedFile = Path.Combine( actorGeneratedPath, "SqlActorPackage_BinPath.dll" );
            File.Exists( generatedFile ).Should().BeTrue();
        }
    }
}
