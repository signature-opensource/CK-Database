using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.Tests
{
    public class SetupRunTests
    {
        [TestCase( TestStoreType.Zip, "Off" )]
        [TestCase( TestStoreType.Directory, "Debug" )]
        public void setup_SqlCallDemoNet20_publish_folder_WORKS_thanks_to_default_runtimeconfig_json_file( TestStoreType type, string logFilter )
        {
            using( TestHelper.Monitor.TemporarilySetMinimalFilter( LogFilter.Parse( logFilter ) ) )
            using( var zip = TestHelper.OpenCKDatabaseZip( type, withNetStandard: true ) )
            {
                var conf = $@"
<Root>
    <CKSetup>
        <BinPaths>
            <BinPath>{TestHelper.EnsurePublishPath( TestHelper.SqlCallDemoNet20 )}</BinPath>
        </BinPaths>
        <EngineAssemblyQualifiedName>CK.Setup.StObjEngine, CK.StObj.Engine</EngineAssemblyQualifiedName>
    </CKSetup>
    <Configuration>
        <BuildAndRegisterConfiguration>
            <AssemblyRegistererConfiguration>
            </AssemblyRegistererConfiguration>
        </BuildAndRegisterConfiguration>
        <BuilderFinalAssemblyConfiguration>

        </BuilderFinalAssemblyConfiguration>
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

    </Configuration>
</Root>
";
                //Facade.DoRun().Should().BeTrue();
            }
        }
    }
}
