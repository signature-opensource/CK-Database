using CK.Core;
using CK.Setup;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                    <DefaultDatabaseConnectionString>{TestHelper.DatabaseTestConnectionString}</DefaultDatabaseConnectionString>
                </Aspect>
            </C>" );

            bool result = TestHelper.WithWeakAssemblyResolver( () =>
            {
                var e = new StObjEngine( TestHelper.Monitor, config );
                return e.Run();
            } );
            Assert.That( result, Is.True );
            string generatedFile = Path.Combine( actorGeneratedPath, "SqlActorPackage_SetupFolder.dll" );
            Assert.That( File.Exists( generatedFile ) );
        }
    }
}
