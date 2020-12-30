using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using static CK.Testing.DBSetupTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class SecondaryRunTests
    {
        /// <summary>
        /// TBI: this test fails when run with all the other tests but succeeds when run alone.
        /// </summary>
        [Test]
        [Explicit]
        public void SqlZonePackage_with_SqlActorPackage_BinPath()
        {
            Assume.That( TestHelper.IsExplicitAllowed, "Press Ctrl key to allow this test to run." );

            var actorGeneratedPath = Path.Combine( TestHelper.BinFolder, "../ForActorOnly" );
            Directory.CreateDirectory( actorGeneratedPath );
            try
            {
                string file1 = Path.Combine( actorGeneratedPath, "SqlActorPackage_BinPath.dll" );
                string file2 = Path.Combine( TestHelper.BinFolder, "SqlActorPackage_BinPath.dll" );
                if( File.Exists( file1 ) ) File.Delete( file1 );
                if( File.Exists( file2 ) ) File.Delete( file2 );

                var config = XElement.Parse( $@"
            <C>
                <GeneratedAssemblyName>SqlActorPackage_BinPath</GeneratedAssemblyName>
                <BinPaths>
                    <BinPath Path=""{TestHelper.BinFolder}"" >
                        <CompileOption>Compile</CompileOption>
                        <Assemblies>
                            <Assembly>SqlActorPackage</Assembly>
                            <Assembly>SqlZonePackage</Assembly>
                        </Assemblies>
                    </BinPath>
                    <BinPath Path=""{actorGeneratedPath}"" >
                        <CompileOption>Compile</CompileOption>
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
                File.Exists( file2 ).Should().BeTrue();
                File.Exists( file1 ).Should().BeTrue();
            }
            finally
            {
                TestHelper.CleanupFolder( actorGeneratedPath );
                Directory.Delete( actorGeneratedPath );
            }
        }
    }
}
