using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Xml;
using System.Xml.Linq;

namespace CK.Setup.Database.Tests
{
    [TestFixture]
    public class FileBased
    {
        [Test]
        public void FileNameParser()
        {
            ParsedFileName result;
            Assert.That( ParsedFileName.TryParse( "Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.ExtraPath, Is.Null );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.IsContent, Is.False );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.None ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "Test.InstallContent.1.2.3.to.1.2.4.sql", @"C:\", true, out result ) );
            Assert.That( result.ExtraPath, Is.EqualTo( @"C:\" ) );
            Assert.That( result.FromVersion, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 4 ) ) );
            Assert.That( result.IsContent, Is.True );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Install ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "Test.Settle", "CK.Test", false, out result ) );
            Assert.That( result.ExtraPath, Is.EqualTo( "CK.Test" ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.IsContent, Is.False );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Settle ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "Test.SettleContent.sql", null, true, out result ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.IsContent, Is.True );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Settle ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

        }


        [Test]
        public void XmlPackage()
        {

            XElement e = XElement.Parse( @"
<SetupPackage FullName=""TheFirstPackageEver"" Versions=""1.2.88, 1.2.4, Old.Name-Is-in.the.Versions = 1.3.4, The.New.Name=1.4.1, 1.5.0"">
    <Requirements Requires=""AnOtherPackage, YetAnotherOne"" RequiredBy=""AnObjectIHook, AnotherObjectIHook"" />
    <Model>
        <Requirements Requires=""AnOtherPackage, YetAnotherOne"" RequiredBy=""AnObjectIHook, AnotherObjectIHook"" />
    </Model>
    <Content>
        <Add FullName=""ContainedItem"" />
        <Add FullName=""AnotherContainedItem"" />
    </Content>
</SetupPackage>
" );
            Package p = FileDiscoverer.ReadPackageFileFormat( e );
            Assert.That( p.VersionList.IsSortedStrict() );
        }


        public class SqlObjectBuilderMock : ISqlObjectBuilder
        {
            public SqlObjectPreParse PreParse( IActivityLogger logger, string text )
            {
                throw new NotImplementedException();
            }

            public ISetupableItem Create( IActivityLogger logger, SqlObjectPreParse preParsed, SetupableItemData data )
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void AllStepsFiles()
        {
            FileDiscoverer discoverer = new FileDiscoverer( new SqlObjectBuilderMock(), DefaultActivityLogger.Empty );
            PackageScriptCollector collector = new PackageScriptCollector();
            Assert.That( discoverer.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "AllSteps" ), collector ), Is.True );

            bool caseDiffer;
            PackageScriptSet scripts = collector.Find( "test", out caseDiffer );
            Assert.That( scripts, Is.Not.Null );
            Assert.That( caseDiffer, Is.True );

            scripts = collector.Find( "Test", out caseDiffer );
            Assert.That( scripts, Is.Not.Null );
            Assert.That( caseDiffer, Is.False );

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.Init, null, new Version( 1, 1, 10 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 1, 9 ) ) );
                Assert.That( v.HasTheNoVersionScript, Is.True );
                CheckScripts( v, "Init.1.1.9.sql", ".Init.sql" );
            }
            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.Init, new Version( 1, 1, 9 ), new Version( 1, 1, 10 ) );
                Assert.That( v.Final, Is.Null );
                Assert.That( v.HasTheNoVersionScript, Is.False, "Nothing is installed: the no version script will not be added." );
                CheckScripts( v );
            }
            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.Init, new Version( 1, 0, 0 ), new Version( 1, 1, 10 ) );
                Assert.That( v.Final, Is.Null );
                Assert.That( v.HasTheNoVersionScript, Is.False, "Nothing is installed: the no version script will not be added." );
                CheckScripts( v );
            }

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.InitContent, null, new Version( 1, 2, 10 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                Assert.That( v.HasTheNoVersionScript, Is.False );
                CheckScripts( v, "Test.InitContent.1.2.3.sql" );
            }

            // Test.Install.1.1.1.to.1.2.3.sql
            // Test.Install.1.1.9.to.1.2.2.sql
            // Test.Install.1.2.3.sql
            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.Install, null, new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                Assert.That( v.HasTheNoVersionScript, Is.False );
                CheckScripts( v, ".Install.1.2.3.sql" );
            }

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.Install, new Version( 1, 0, 0 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".Install.1.1.1.to.1.2.3.sql" );
            }

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.Install, new Version( 1, 1, 2 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 2 ) ) );
                CheckScripts( v, ".Install.1.1.9.to.1.2.2.sql" );
            }

            // Test.InstallContent.1.1.5.to.1.1.6.sql
            // Test.InstallContent.1.1.6.to.1.1.7.sql
            // Test.InstallContent.1.1.7.to.1.1.8.sql
            // Test.InstallContent.1.1.7.to.1.2.1.sql  ->
            // Test.InstallContent.1.1.8.to.1.1.9.sql    |
            // Test.InstallContent.1.1.9.to.1.2.0.sql    |
            // Test.InstallContent.1.2.0.to.1.2.1.sql    |
            // Test.InstallContent.1.2.1.to.1.2.2.sql <-
            // Test.InstallContent.1.2.2.to.1.2.3.sql
            // Test.InstallContent.1.2.3.sql
            // Test.InstallContent.sql

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.InstallContent, new Version( 1, 1, 5 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.1.5.to.1.1.6.sql", ".InstallContent.1.1.6.to.1.1.7.sql", ".InstallContent.1.1.7.to.1.2.1.sql", ".InstallContent.1.2.1.to.1.2.2.sql", ".InstallContent.1.2.2.to.1.2.3.sql", ".InstallContent.sql" );
            }

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.InstallContent, new Version( 1, 1, 7 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.1.7.to.1.2.1.sql", ".InstallContent.1.2.1.to.1.2.2.sql", ".InstallContent.1.2.2.to.1.2.3.sql", ".InstallContent.sql" );
            }

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.InstallContent, new Version( 1, 1, 8 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.1.8.to.1.1.9.sql", ".InstallContent.1.1.9.to.1.2.0.sql", ".InstallContent.1.2.0.to.1.2.1.sql", ".InstallContent.1.2.1.to.1.2.2.sql", ".InstallContent.1.2.2.to.1.2.3.sql", ".InstallContent.sql" );
            }

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.InstallContent, new Version( 1, 1, 7 ), new Version( 1, 1, 9 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 1, 9 ) ) );
                CheckScripts( v, ".InstallContent.1.1.7.to.1.1.8.sql", ".InstallContent.1.1.8.to.1.1.9.sql", ".InstallContent.sql" );
            }

            {
                var v = scripts.GetScriptVector( "sql", SetupCallContainerStep.InstallContent, null, new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.2.3.sql", ".InstallContent.sql" );
            }

        }

        void CheckScripts( PackageScriptVector v, params string[] suffixes )
        {
            var scriptNames = v.Scripts.Select( p => p.Name.FileName );
            var zipped = scriptNames.Zip( suffixes, ( f, suffix ) => new Tuple<string,string>( f, suffix ) );
            Assert.That( zipped.All( t => t.Item1.EndsWith( t.Item2 ) ) && suffixes.Count() == scriptNames.Count() ); 
        }
    }
}
