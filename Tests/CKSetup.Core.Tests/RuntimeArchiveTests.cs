using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup.Tests
{
    [TestFixture]
    public class RuntimeArchiveTests
    {
        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void adding_setupable_runtimes_files_to_zip_is_not_possible_unless_its_embedded_are_also_added( TestStoreType type )
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath ) )
            {
                zip.Should().NotBeNull();

                var fSetupableRT = BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.SetupableRuntime461 );
                var fSetupableM = BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.SetupableModel461 );
                var fStObjRT = BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.StObjRuntime461 );
                var fStObjM = BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.StObjModel461 );
                fSetupableRT.Should().NotBeNull();
                fSetupableM.Should().NotBeNull();
                fStObjRT.Should().NotBeNull();
                fStObjM.Should().NotBeNull();

                zip.CreateLocalImporter().AddComponent( fSetupableRT ).Import().Should().BeFalse();
                zip.CreateLocalImporter().AddComponent( fSetupableRT, fStObjRT ).Import().Should().BeFalse();
                zip.CreateLocalImporter().AddComponent( fSetupableRT, fStObjRT, fSetupableM ).Import().Should().BeFalse();
                zip.CreateLocalImporter().AddComponent( fSetupableRT, fStObjRT, fSetupableM, fStObjM )
                    .Import().Should().BeTrue();
            }
            StoreContent c = new StoreContent( zipPath );
            c.Db.Should().NotBeNull();

            var setupableRuntime = c.ComponentDB
                                    .Components
                                    .Single( x => x.Name == "CK.Setupable.Runtime"
                                                  && x.TargetFramework == TargetFramework.Net461 );
            c.ComponentDB.Components.Count.Should().Be( 4 );
            c.AllComponentFilesShouldBeStored( setupableRuntime );
        }

        [TestCase( TestStoreType.Zip, true )]
        [TestCase( TestStoreType.Directory, false )]
        public void adding_both_stobj_runtime_and_stobj_model_in_net461( TestStoreType type, bool runtimeFirst )
        {
            string zipPath = TestHelper.GetTestZipPath( type, runtimeFirst ? ".runtimeFirst" : ".modelFirst" );
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath ) )
            {
                zip.Should().NotBeNull();
                if( runtimeFirst )
                {
                    zip.CreateLocalImporter().AddComponent( 
                        BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.StObjRuntime461 ), 
                        BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.StObjModel461 ) )
                        .Import().Should().BeTrue();
                }
                else
                {
                    zip.CreateLocalImporter().AddComponent(
                        BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.StObjModel461 ),
                        BinFolder.ReadBinFolder( TestHelper.Monitor, TestHelper.StObjRuntime461 ) )
                        .Import().Should().BeTrue();
                }
            }
            StoreContent c = new StoreContent( zipPath );
            c.Db.Should().NotBeNull();
            var stObjRuntime = c.ComponentDB.Components.Single( e => e.Name == "CK.StObj.Runtime" && e.TargetFramework == TargetFramework.Net461 );
            var stObjModel = c.ComponentDB.Components.Single( e => e.Name == "CK.StObj.Model" && e.TargetFramework == TargetFramework.Net461 );

            // Only runtimes files remains: Model files are removed.
            c.ComponentShouldContainFiles( stObjRuntime, "CK.StObj.Runtime.dll" );
            c.ComponentShouldNotContainFiles( stObjRuntime, 
                "CK.StObj.Model.dll", 
                "CK.ActivityMonitor.dll" );

            // Model files in .Net framework are not stored.
            c.NoComponentFilesShouldBeStored( stObjModel );
        }

        [TestCase( TestStoreType.Zip )]
        [TestCase( TestStoreType.Directory )]
        public void importing_exporting_components( TestStoreType type )
        {
            string zipPath = TestHelper.GetCleanTestZipPath( type );
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.Monitor, zipPath ) )
            using( RuntimeArchive realZip = TestHelper.OpenCKDatabaseZip( type ) )
            using( Stream buffer = new MemoryStream() )
            {
                zip.Export( c => true, buffer );
                buffer.Position.Should().BeLessThan( 100, "Only marker contents." );

                buffer.Position = 0;
                realZip.Export( c => c.Name == "CK.Setupable.Engine", buffer );
                buffer.Position.Should().BeGreaterThan( 100 );
                buffer.WriteByte( 251 );
                buffer.Position = 0;
                zip.ImportComponents( buffer, new FakeRemote( realZip ) ).Should().BeTrue();
                buffer.ReadByte().Should().Be( 251 );
            }
            StoreContent content = new StoreContent( zipPath );
            var engines = content.Db.Elements( "Component" ).ToArray();
            engines.Select( c => c.Attribute( "Name" ).Value )
                    .All( n => n == "CK.Setupable.Engine" )
                    .Should().BeTrue();

            // CK.Setupable.Engine.dll (net461) and/or
            // CK.Setupable.Engine.dll (net20) and CK.Setupable.Engine.deps.json.
            content.FileEntries.Should().HaveCount( n => n > 0 && n <= 3 );
        }


    }
}
