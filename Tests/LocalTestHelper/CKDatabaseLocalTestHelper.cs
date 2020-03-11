using CK.Core;
using CK.Testing.CKDatabaseLocal;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Linq;

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
            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Model/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp3.1" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.Setupable.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp3.1" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Model/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp3.1" );

            yield return _dbSetup.SolutionFolder.Combine( $"CK.SqlServer.Setup.Engine/bin/{_dbSetup.BuildConfiguration}/netcoreapp3.1" );
        }

        IEnumerable<NormalizedPath> ICKDatabaseLocalTestHelperCore.SqlActorPackageComponentsPaths => GetSqlActorPackageComponentsPaths();
        IEnumerable<NormalizedPath> GetSqlActorPackageComponentsPaths()
        {
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlActorPackage/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlActorPackage.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp3.1" );
        }

        IEnumerable<NormalizedPath> ICKDatabaseLocalTestHelperCore.SqlZonePackageComponentsPaths => GetSqlZonePackageComponentsPaths();
        IEnumerable<NormalizedPath> GetSqlZonePackageComponentsPaths()
        {
            foreach( var p in GetSqlActorPackageComponentsPaths() ) yield return p;
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlZonePackage/bin/{_dbSetup.BuildConfiguration}/netstandard2.0" );
            yield return _dbSetup.SolutionFolder.Combine( $"Tests/BasicModels/SqlZonePackage.Runtime/bin/{_dbSetup.BuildConfiguration}/netcoreapp3.1" );
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
            }
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
