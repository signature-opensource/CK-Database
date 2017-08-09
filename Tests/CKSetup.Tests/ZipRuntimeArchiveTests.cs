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
    public class ZipRuntimeArchiveTests
    {
        [Test]
        public void adding_setupable_runtimes_files_to_zip()
        {
            string zipPath = TestHelper.GetCleanTestZipPath();
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            {
                zip.Should().NotBeNull();
                var folder = BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableRuntime461Path );
                folder.Should().NotBeNull();
                zip.AddComponent( folder ).Should().BeTrue();
            }
            ZipContent c = new ZipContent( zipPath );
            c.Db.Should().NotBeNull();

            var setupableRuntime = c.ComponentDB
                                    .Components
                                    .Single( x => x.Name == "CK.Setupable.Runtime"
                                                  && x.TargetFramework == TargetFramework.Net461 );
            c.AllComponentFilesShouldBeStored( setupableRuntime );
            c.ComponentShouldContainFiles( setupableRuntime, 
                "CK.StObj.Model.dll",
                "CK.StObj.Runtime.dll",
                "CK.Setupable.Model.dll",
                "CK.Setupable.Runtime.dll",
                "CK.ActivityMonitor.dll" );
        }

        [TestCase( true )]
        [TestCase( false )]
        public void adding_both_setupable_runtime_and_setupable_model( bool runtimeFirst )
        {
            string zipPath = TestHelper.GetTestZipPath( runtimeFirst ? ".runtimeFirst" : ".modelFirst" );
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            {
                if( runtimeFirst )
                {
                    zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableRuntime461Path ) ).Should().BeTrue();
                    zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableModel461Path ) ).Should().BeTrue();
                }
                else
                {
                    zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableModel461Path ) ).Should().BeTrue();
                    zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableRuntime461Path ) ).Should().BeTrue();
                }
            }
            ZipContent c = new ZipContent( zipPath );
            c.Db.Should().NotBeNull();
            var setupableRuntime = c.ComponentDB.Components.Single( e => e.Name == "CK.Setupable.Runtime" && e.TargetFramework == TargetFramework.Net461 );
            var setupableModel = c.ComponentDB.Components.Single( e => e.Name == "CK.Setupable.Model" && e.TargetFramework == TargetFramework.Net461 );

            // Model files are not stored.
            setupableModel.Files.Should().BeEmpty();

            // Only runtimes files remains: Model files are removed.
            c.ComponentShouldContainFiles( setupableRuntime,
                "CK.StObj.Runtime.dll",
                "CK.Setupable.Runtime.dll" );
            c.ComponentShouldNotContainFiles( setupableRuntime,
                "CK.Setupable.Model.dll", 
                "CK.StObj.Model.dll", 
                "CK.ActivityMonitor.dll" );
        }

        [Test]
        public void ComponentDB_maintains_a_MissingRegistrations_set_of_ComponentRef()
        {
            string zipPath = TestHelper.GetCleanTestZipPath();
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            {
                zip.SimpleMissingRegistrations.Should().BeEmpty();
                // Add the terminal model: SqlActorPackage
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageModel461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "SqlActorPackage.Runtime",
                        "CK.StObj.Model",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.StObj.Model: it requires now its Runtime.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.StObjModel461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "SqlActorPackage.Runtime",
                        "CK.StObj.Runtime",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.StObj.Runtime: it requires now its Engine.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.StObjRuntime461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "SqlActorPackage.Runtime",
                        "CK.StObj.Engine",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.SqlServer.Setup.Engine: it embeds its Runtime that should now be registered
                // as well as the CK.Setupable.Engine and Runtime and CK.StObj.Runtime.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlServerSetupEngine461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "SqlActorPackage.Runtime",
                        "CK.StObj.Engine",
                        "CK.SqlServer.Setup.Runtime",
                        "CK.Setupable.Engine",
                        "CK.Setupable.Runtime",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the SqlActorPackage.Runtime.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageRuntime461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "CK.StObj.Engine",
                        "CK.SqlServer.Setup.Runtime",
                        "CK.Setupable.Engine",
                        "CK.Setupable.Runtime",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.SqlServer.Setup.Runtime.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlServerSetupRuntime461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "CK.StObj.Engine",
                        "CK.Setupable.Engine",
                        "CK.Setupable.Runtime",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.StObj.Engine.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.StObjEngine461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "CK.Setupable.Engine",
                        "CK.Setupable.Runtime",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.Setupable.Engine.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableEngine461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "CK.Setupable.Runtime",
                        "CK.Setupable.Model",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.Setupable.Model.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableModel461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "CK.Setupable.Runtime",
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.Setupable.Runtime.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableRuntime461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Select( m => m.UseName ).ShouldBeEquivalentTo( new[]
                    {
                        "CK.SqlServer.Setup.Model"
                    } );
                // Adds the CK.SqlServer.Setup.Model.
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlServerSetupModel461Path ) ).Should().BeTrue();
                zip.SimpleMissingRegistrations.Should().BeEmpty();
            }
            string zipPathRev = TestHelper.GetCleanTestZipPath( "Reversed" );
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPathRev ) )
            {
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlServerSetupModel461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableRuntime461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableModel461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableEngine461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.StObjEngine461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlServerSetupRuntime461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageRuntime461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlServerSetupEngine461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.StObjRuntime461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.StObjModel461Path ) ).Should().BeTrue();
                zip.AddComponent( BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SqlActorPackageModel461Path ) ).Should().BeTrue();
            }
            ZipContent c1 = new ZipContent( zipPath );
            ZipContent c2 = new ZipContent( zipPathRev );

            XNode.EqualityComparer.Equals( c1.Db, c2.Db ).Should().BeTrue();
            c1.Files.ShouldBeEquivalentTo( c2.Files );
        }


        [Test]
        public void importing_exporting_components()
        {
            string zipPath = TestHelper.GetCleanTestZipPath();
            using( RuntimeArchive zip = RuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            using( RuntimeArchive realZip = TestHelper.OpenCKDatabaseZip() )
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
            ZipContent content = new ZipContent( zipPath );
            content.Db.Elements( "Component" ).Single().Attribute( "Name" ).Value.Should().Be( "CK.Setupable.Engine" );
            content.Files.Count.Should().Be( 1 );
        }


    }
}
