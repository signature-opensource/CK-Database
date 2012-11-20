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
        public void XmlPackage()
        {

            XElement e = XElement.Parse( @"
<SetupPackage FullName=""TheFirstPackageEver"" Versions=""1.2.88, 1.2.4, Old.Name-Is-in.the.Versions = 1.3.4, The.New.Name=1.4.1, 1.5.0"">
    <Requirements Requires=""AnOtherPackage, YetAnotherOne"" RequiredBy=""AnObjectIHook, AnotherObjectIHook"" />
    <Model>
        <Requirements Requires=""AnOtherPackage, db^YetAnotherOne"" RequiredBy=""AnObjectIHook, AnotherObjectIHook"" />
    </Model>
    <Content>
        <Add FullName=""ContainedItem"" />
        <Add FullName=""AnotherContainedItem"" />
    </Content>
</SetupPackage>
" );
            DynamicPackageItem p = SqlFileDiscoverer.ReadPackageFileFormat( e, "C", null );
            Assert.That( p.VersionList.IsSortedStrict() );
            Assert.That( p.FullName, Is.EqualTo( "[C]TheFirstPackageEver" ) );
            Assert.That( p.Requires[0].FullName, Is.EqualTo( "AnOtherPackage" ) );
            Assert.That( p.Requires[1].FullName, Is.EqualTo( "db^YetAnotherOne" ) );


            IDependentItemContainer c = p as IDependentItemContainer;
            Assert.That( c.Requires.ElementAt(0).FullName, Is.EqualTo( "[C]AnOtherPackage" ) );
            Assert.That( c.Requires.ElementAt(1).FullName, Is.EqualTo( "[C]db^YetAnotherOne" ) );
        }


        class SqlObjectParserStub : ISqlObjectParser
        {
            public IDependentProtoItem Create( IActivityLogger logger, IContextLocName externalName, string text )
            {
                throw new NotImplementedException();
            }
        }

        class SqlScriptTypeHandler : ScriptTypeHandler
        {
            public SqlScriptTypeHandler()
            {
                RegisterSource( "file-sql" );
            }

            protected override IScriptExecutor CreateExecutor( IActivityLogger logger, SetupDriver driver )
            {
                throw new NotImplementedException();
            }

            protected override void ReleaseExecutor( IActivityLogger logger, IScriptExecutor executor )
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void FromOpenTo()
        {
            ScriptTypeManager typeManager = new ScriptTypeManager();
            typeManager.Register( new SqlScriptTypeHandler() );
            ScriptCollector collector = new ScriptCollector( typeManager );
            SqlFileDiscoverer discoverer = new SqlFileDiscoverer( new SqlObjectParserStub(), TestHelper.Logger );
            Assert.That( discoverer.DiscoverSqlFiles( null, null, TestHelper.GetScriptsFolder( "FromOpenTo" ), new DependentProtoItemCollector(), collector ), Is.True );

            bool caseDiffer;
            ScriptSet scripts = collector.Find( "Test", out caseDiffer );
            Assert.That( scripts, Is.Not.Null );
            Assert.That( caseDiffer, Is.False );

            var scriptsForSql = scripts.ScriptsByHandlers.Single( h => h.Handler.HandlerName == "Sql" );
            {
                {
                    var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, null, new Version( 1, 0, 1 ) );
                    Assert.That( v.Final, Is.EqualTo( new Version( 1, 0, 1 ) ) );
                    Assert.That( v.HasTheNoVersionScript, Is.False );
                    CheckScripts( v, ".Install.1.0.0.sql", ".Install.1.0.0.to.1.0.1.sql" );
                }
                {
                    var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, null, new Version( 1, 0, 2 ) );
                    Assert.That( v.Final, Is.EqualTo( new Version( 1, 0, 2 ) ) );
                    Assert.That( v.HasTheNoVersionScript, Is.False );
                    CheckScripts( v, ".Install.1.0.0.sql", ".Install.1.0.0.to.1.0.1.sql", ".Install.1.0.1.to.1.0.2.sql" );
                }
                {
                    var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, null, new Version( 1, 0, 3 ) );
                    Assert.That( v.Final, Is.EqualTo( new Version( 1, 0, 3 ) ) );
                    Assert.That( v.HasTheNoVersionScript, Is.False );
                    CheckScripts( v, ".Install.1.0.0.sql", ".Install.1.0.0.to.1.0.1.sql", ".Install.1.0.1.to.1.0.2.sql", ".Install.1.0.2.to.1.0.3.sql" );
                }
                {
                    var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, null, new Version( 1, 0, 4 ) );
                    Assert.That( v.Final, Is.EqualTo( new Version( 1, 0, 4 ) ) );
                    Assert.That( v.HasTheNoVersionScript, Is.False );
                    CheckScripts( v, ".Install.1.0.4.sql" );
                }
                {
                    var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, null, new Version( 1, 0, 5 ) );
                    Assert.That( v.Final, Is.EqualTo( new Version( 1, 0, 5 ) ) );
                    Assert.That( v.HasTheNoVersionScript, Is.False );
                    CheckScripts( v, ".Install.1.0.4.sql", ".Install.1.0.4.to.1.0.5.sql" );
                }
            }
        }

        [Test]
        public void GetScriptVectorAllStepsFiles()
        {
            ScriptTypeManager typeManager = new ScriptTypeManager();
            typeManager.Register( new SqlScriptTypeHandler() );
            ScriptCollector collector = new ScriptCollector( typeManager );
            
            SqlFileDiscoverer discoverer = new SqlFileDiscoverer( new SqlObjectParserStub(), TestHelper.Logger );

            Assert.That( discoverer.DiscoverSqlFiles( null, null, TestHelper.GetScriptsFolder( "AllSteps" ), new DependentProtoItemCollector(), collector ), Is.True );

            bool caseDiffer;
            ScriptSet scripts = collector.Find( "test", out caseDiffer );
            Assert.That( scripts, Is.Not.Null );
            Assert.That( caseDiffer, Is.True, "Files are 'Test.xxx', not 'test.xxx'." );

            scripts = collector.Find( "Test", out caseDiffer );
            Assert.That( scripts, Is.Not.Null );
            Assert.That( caseDiffer, Is.False );

            var scriptsForSql = scripts.ScriptsByHandlers.Single( h => h.Handler.HandlerName == "Sql" );
            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Init, null, new Version( 1, 1, 10 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 1, 9 ) ) );
                Assert.That( v.HasTheNoVersionScript, Is.True );
                CheckScripts( v, "Init.1.1.9.sql", ".Init.sql" );
            }
            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Init, new Version( 1, 1, 9 ), new Version( 1, 1, 10 ) );
                Assert.That( v, Is.Null, "Nothing is installed: the no version script will not be added." );
            }
            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Init, new Version( 1, 0, 0 ), new Version( 1, 1, 10 ) );
                Assert.That( v, Is.Null, "Nothing is installed: the no version script will not be added." );
            }

            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.InitContent, null, new Version( 1, 2, 10 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                Assert.That( v.HasTheNoVersionScript, Is.False );
                CheckScripts( v, "Test.InitContent.1.2.3.sql" );
            }

            // Test.Install.1.1.1.to.1.2.3.sql
            // Test.Install.1.1.9.to.1.2.2.sql
            // Test.Install.1.2.3.sql
            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, null, new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                Assert.That( v.HasTheNoVersionScript, Is.False );
                CheckScripts( v, ".Install.1.2.3.sql" );
            }

            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, new Version( 1, 0, 0 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".Install.1.1.1.to.1.2.3.sql" );
            }

            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.Install, new Version( 1, 1, 2 ), new Version( 1, 2, 3 ) );
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
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.InstallContent, new Version( 1, 1, 5 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.1.5.to.1.1.6.sql", ".InstallContent.1.1.6.to.1.1.7.sql", ".InstallContent.1.1.7.to.1.2.1.sql", ".InstallContent.1.2.1.to.1.2.2.sql", ".InstallContent.1.2.2.to.1.2.3.sql", ".InstallContent.sql" );
            }

            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.InstallContent, new Version( 1, 1, 7 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.1.7.to.1.2.1.sql", ".InstallContent.1.2.1.to.1.2.2.sql", ".InstallContent.1.2.2.to.1.2.3.sql", ".InstallContent.sql" );
            }

            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.InstallContent, new Version( 1, 1, 8 ), new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.1.8.to.1.1.9.sql", ".InstallContent.1.1.9.to.1.2.0.sql", ".InstallContent.1.2.0.to.1.2.1.sql", ".InstallContent.1.2.1.to.1.2.2.sql", ".InstallContent.1.2.2.to.1.2.3.sql", ".InstallContent.sql" );
            }

            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.InstallContent, new Version( 1, 1, 7 ), new Version( 1, 1, 9 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 1, 9 ) ) );
                CheckScripts( v, ".InstallContent.1.1.7.to.1.1.8.sql", ".InstallContent.1.1.8.to.1.1.9.sql", ".InstallContent.sql" );
            }

            {
                var v = scriptsForSql.GetScriptVector( SetupCallGroupStep.InstallContent, null, new Version( 1, 2, 3 ) );
                Assert.That( v.Final, Is.EqualTo( new Version( 1, 2, 3 ) ) );
                CheckScripts( v, ".InstallContent.1.2.3.sql", ".InstallContent.sql" );
            }

        }

        void CheckScripts( TypedScriptVector v, params string[] suffixes )
        {
            var scriptNames = v.Scripts.Select( p => p.Script.Name.FileName );
            var zipped = scriptNames.Zip( suffixes, ( f, suffix ) => new Tuple<string,string>( f, suffix ) );
            Assert.That( zipped.All( t => t.Item1.EndsWith( t.Item2 ) ) && suffixes.Count() == scriptNames.Count() ); 
        }
    }
}
