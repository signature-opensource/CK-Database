using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup.Tests
{
    [TestFixture]
    public class SetupRunTests
    {
        [Test]
        public void setup_SqlCallDemoNet20_publish_folder_WORKS_thanks_to_default_runtimeconfig_json_file()
        {
            using( TestHelper.Monitor.TemporarilySetMinimalFilter( LogFilter.Debug ) )
            using( var archive = TestHelper.OpenCKDatabaseZip( TestStoreType.Directory, withNetStandard: true ) )
            {
                var conf = $@"
<Root>
    <CKSetup>
        <WorkingDirectory></WorkingDirectory>
        <BinPaths>
            <BinPath>{TestHelper.EnsurePublishPath( TestHelper.SqlCallDemoNet20 )}</BinPath>
        </BinPaths>
        <EngineAssemblyQualifiedName>CK.Setup.StObjEngine, CK.StObj.Engine</EngineAssemblyQualifiedName>
    </CKSetup>
    <StObjEngineConfiguration>
        <TraceDependencySorterInput>false</TraceDependencySorterInput>
        <TraceDependencySorterOutput>false</TraceDependencySorterOutput>
        <RevertOrderingNames>false</RevertOrderingNames>
        <GenerateAppContextAssembly>true</GenerateAppContextAssembly>
        <GeneratedAssemblyName>CK.StObj.AutoAssembly</GeneratedAssemblyName>
        <Aspect Type=""CK.Setup.SetupableAspectConfiguration, CK.Setupable.Model"" >
            <TraceDependencySorterInput>false</TraceDependencySorterInput>
            <TraceDependencySorterOutput>false</TraceDependencySorterOutput>
            <RevertOrderingNames>false</RevertOrderingNames>
        </Aspect>
        <Aspect Type=""CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model"" >
            <DefaultDatabaseConnectionString>{TestHelper.GetConnectionString( "CKDB_TEST_SqlCallDemo" )}</DefaultDatabaseConnectionString>
            <GlobalResolution>false</GlobalResolution>
            <IgnoreMissingDependencyIsError>true</IgnoreMissingDependencyIsError>
        </Aspect>
    </StObjEngineConfiguration>
</Root>
";
                var setupConfig = new SetupConfiguration( XDocument.Parse( conf ) );
                Facade.DoRun( TestHelper.Monitor, archive, setupConfig ).Should().BeTrue();
            }
        }

        [Test]
        public void setup_SqlActorPackageModel461()
        {
            using( TestHelper.Monitor.TemporarilySetMinimalFilter( LogFilter.Debug ) )
            {
                string zipPath = TestHelper.GetCleanTestZipPath( TestStoreType.Directory );
                using( var archive = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath ) )
                using( var remoteZip = TestHelper.OpenCKDatabaseZip( TestStoreType.Directory ) )
                {
                    var missingImporter = new FakeRemote( remoteZip );
                    archive.CreateLocalImporter( missingImporter ).AddComponent(
                        BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.SqlActorPackageModel461 ),
                        BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.SqlActorPackageRuntime461 ) )
                        .Import()
                        .Should().BeTrue();

                    var conf = $@"
<Root>
    <CKSetup>
        <BinPaths>
            <BinPath>{TestHelper.SqlActorPackageModel461}</BinPath>
        </BinPaths>
        <EngineAssemblyQualifiedName>CK.Setup.StObjEngine, CK.StObj.Engine</EngineAssemblyQualifiedName>
    </CKSetup>
    <StObjEngineConfiguration>
        <GeneratedAssemblyName>SqlActorPackage.Generated.ByCKSetup</GeneratedAssemblyName>
        <Aspect Type=""CK.Setup.SetupableAspectConfiguration, CK.Setupable.Model"" />
        <Aspect Type=""CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model"" >
            <DefaultDatabaseConnectionString>{TestHelper.GetConnectionString( "CKDB_TEST_SqlActorPackage" )}</DefaultDatabaseConnectionString>
        </Aspect>
    </StObjEngineConfiguration>
</Root>
";
                    var setupConfig = new SetupConfiguration( XDocument.Parse( conf ) );
                    Facade.DoRun( TestHelper.Monitor, archive, setupConfig, missingImporter ).Should().BeTrue();
                }
            }
        }
    }
}
