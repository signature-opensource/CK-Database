using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CKSetup.Tests
{
    [TestFixture]
    public class BinFileInfoTests
    {
        [Test]
        public void adding_setupable_runtimes_files_to_zip()
        {
            string zipPath = TestHelper.GetTestZipPath();
            using( ZipRuntimeArchive zip = ZipRuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
            {
                zip.Should().NotBeNull();
                var folder = BinFolder.ReadBinFolder( TestHelper.ConsoleMonitor, TestHelper.SetupableRuntime461Path );
                folder.Should().NotBeNull();
                zip.AddComponent( folder ).Should().BeTrue();
            }
            ZipContent c = new ZipContent( zipPath );
            c.Db.Should().NotBeNull();
            var setupableRuntimeFiles = c.Files.Where( e => e.ComponentName == "CK.Setupable.Runtime" && e.Framework == "Net461" );
            setupableRuntimeFiles.Should().Contain( f => f.FilePath == "CK.StObj.Model.dll" );
            setupableRuntimeFiles.Should().Contain( f => f.FilePath == "CK.StObj.Runtime.dll" );
            setupableRuntimeFiles.Should().Contain( f => f.FilePath == "CK.Setupable.Model.dll" );
            setupableRuntimeFiles.Should().Contain( f => f.FilePath == "CK.Setupable.Runtime.dll" );
            setupableRuntimeFiles.Should().Contain( f => f.FilePath == "CK.ActivityMonitor.dll" );
        }

        [TestCase( true )]
        [TestCase( false )]
        public void adding_both_setupable_runtime_and_setupable_model( bool runtimeFirst )
        {
            string zipPath = TestHelper.GetTestZipPath( runtimeFirst ? ".runtimeFirst" : ".modelFirst" );
            using( ZipRuntimeArchive zip = ZipRuntimeArchive.OpenOrCreate( TestHelper.ConsoleMonitor, zipPath ) )
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
            var setupableRuntimeFiles = c.Files.Where( e => e.ComponentName == "CK.Setupable.Runtime" && e.Framework == "Net461" );
            var setupableModelFiles = c.Files.Where( e => e.ComponentName == "CK.Setupable.Model" && e.Framework == "Net461" );
            
            // Model files are not stored.
            setupableModelFiles.Should().BeEmpty();
            
            // Only runtimes files remains: Model files are removed.
            setupableRuntimeFiles.Should().Contain( f => f.FilePath == "CK.StObj.Runtime.dll" );
            setupableRuntimeFiles.Should().Contain( f => f.FilePath == "CK.Setupable.Runtime.dll" );
            setupableModelFiles.Should().NotContain( f => f.FilePath == "CK.Setupable.Model.dll" );
            setupableModelFiles.Should().NotContain( f => f.FilePath == "CK.StObj.Model.dll" );
            setupableModelFiles.Should().NotContain( f => f.FilePath == "CK.ActivityMonitor.dll" );
        }
    }
}
