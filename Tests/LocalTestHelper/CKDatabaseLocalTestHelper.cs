using CK.Core;
using CK.Setup;
using CK.Testing;
using CK.Testing.CKDatabaseLocal;
using CK.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Testing
{
    public class CKDatabaseLocalTestHelper : ICKDatabaseLocalTestHelperCore
    {
        readonly IDBSetupTestHelper _dbSetup;

        internal CKDatabaseLocalTestHelper( ITestHelperConfiguration config, IDBSetupTestHelper ckSetup )
        {
            _dbSetup = ckSetup;
            _dbSetup.CKSetup.InitializeStorePath += OnInitializeStorePath;
        }

        IEnumerable<NormalizedPath> ICKDatabaseLocalTestHelperCore.CKDatabaseComponentsPaths => GetCKDatabaseComponentsPaths();
        IEnumerable<NormalizedPath> GetCKDatabaseComponentsPaths()
        {
            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Model/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Model/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Runtime/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.StObj.Engine/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Model/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Model/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Runtime/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Engine/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Model/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Model/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Runtime/bin/{_dbSetup.BuildConfiguration}/net461" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Engine/bin/{_dbSetup.BuildConfiguration}/net461" );
        }

        IEnumerable<NormalizedPath> ICKDatabaseLocalTestHelperCore.SqlActorPackageComponentsPaths => GetSqlActorPackageComponentsPaths();
        IEnumerable<NormalizedPath> GetSqlActorPackageComponentsPaths()
        {
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlActorPackage/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlActorPackage/bin/{_dbSetup.BuildConfiguration}/net461" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlActorPackage.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlActorPackage.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlActorPackage.Runtime/bin/{_dbSetup.BuildConfiguration}/net461" );
        }

        IEnumerable<NormalizedPath> ICKDatabaseLocalTestHelperCore.SqlZonePackageComponentsPaths => GetSqlZonePackageComponentsPaths();
        IEnumerable<NormalizedPath> GetSqlZonePackageComponentsPaths()
        {
            foreach( var p in GetSqlActorPackageComponentsPaths() ) yield return p;
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlZonePackage/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlZonePackage/bin/{_dbSetup.BuildConfiguration}/net461" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlZonePackage.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlZonePackage.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp2.1" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlZonePackage.Runtime/bin/{_dbSetup.BuildConfiguration}/net461" );
        }

        IEnumerable<NormalizedPath> ICKDatabaseLocalTestHelperCore.AllLocalComponentsPaths => GetAllLocalComponentsPaths();
        IEnumerable<NormalizedPath> GetAllLocalComponentsPaths()
        {
            foreach( var p in GetCKDatabaseComponentsPaths() ) yield return p;
            foreach( var p in GetSqlZonePackageComponentsPaths() ) yield return p;
        }

        void OnInitializeStorePath( object sender, CKSetup.StorePathInitializationEventArgs e )
        {
            if( e.StorePath == _dbSetup.SolutionFolder.Combine("Tests/LocalTestHelper/LocalTestStore") )
            {
                using( _dbSetup.Monitor.OpenInfo( $"LocalHelper initializing Tests/LocalTestHelper/LocalTestStore." ) )
                {
                    var dirty = GetAllLocalComponentsPaths().Where( p => IsPublishedRequired( p ) ).ToList();
                    if( dirty.Any() )
                    {
                        _dbSetup.Monitor.Info( $"Dirty components: {dirty.Select( p => p.ToString() ).Concatenate()}" );
                        _dbSetup.Monitor.Info( $"Publishing all of them." );
                        _dbSetup.CKSetup.RemoveComponentsFromStore(
                                            c => c.Version == CSemVer.SVersion.ZeroVersion,
                                            storePath: e.StorePath );
                        if( !_dbSetup.CKSetup.PublishAndAddComponentFoldersToStore(
                                                GetAllLocalComponentsPaths().Select( p => p.ToString() ),
                                                storePath: e.StorePath ) )
                        {
                            throw new InvalidOperationException( "Unable to add CK-Database components to Tests/LocalTestHelper/LocalTestStore." );
                        }
                    }
                    else _dbSetup.Monitor.Info( "All components are already published." );
                }
            }
        }

        bool IsPublishedRequired( string pathToFramework )
        {
            NormalizedPath path = Path.GetFullPath( pathToFramework );
            var framework = path.LastPart;
            if( !framework.StartsWith( "netcoreapp", StringComparison.OrdinalIgnoreCase ) ) return false;
            var publishPath = path.AppendPart( "publish" );
            // If there is no publish folder, we need to publish.
            if( !Directory.Exists( publishPath ) ) return true;
            var projectName = path.Parts[path.Parts.Count - 4];

            // If we don't find a .dll or .exe with the projectName, in doubt, we need to publish.
            var dllOrExeName = path.AppendPart( projectName );
            string source = dllOrExeName + ".dll";
            if( !File.Exists( source ) )
            {
                source = dllOrExeName + ".exe";
                if( !File.Exists( source ) ) return false;
            }
            var pubName = publishPath.AppendPart( Path.GetFileName(source) );
            if( !File.Exists( pubName ) ) return true;

            DateTime sourceTime = File.GetLastWriteTimeUtc( source );
            DateTime pubTime = File.GetLastWriteTimeUtc( pubName );
            return pubTime < sourceTime;
        }

        void ICKDatabaseLocalTestHelperCore.DeleteAllLocalComponentsPublishedFolders()
        {
            using( _dbSetup.Monitor.OpenInfo( "Deleting published Setup dependencies" ) )
            {
                foreach( var p in GetAllLocalComponentsPaths() )
                {
                    if( p.LastPart.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) )
                    {
                        _dbSetup.CleanupFolder( p.Combine( "publish" ) );
                    }
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="ICKDatabaseLocalTestHelper"/> default implementation.
        /// </summary>
        public static ICKDatabaseLocalTestHelper TestHelper => TestHelperResolver.Default.Resolve<ICKDatabaseLocalTestHelper>();

    }
}
