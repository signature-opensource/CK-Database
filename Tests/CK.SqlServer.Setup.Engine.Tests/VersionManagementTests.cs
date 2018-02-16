using CK.Core;
using CK.Setup;
using CK.Testing;
using CK.Text;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class VersionManagementTests
    {
        static SqlServerDatabaseOptions _db = new SqlServerDatabaseOptions() { DatabaseName = "TEST_SetupEngine_Version" };
        static SqlManager _manager = new SqlManager( TestHelper.Monitor );
        static SqlVersionedItemReader _reader = new SqlVersionedItemReader( _manager );
        static SqlVersionedItemWriter _writer = new SqlVersionedItemWriter( _manager );

        [SetUp]
        public void OpenConnectionAndCleanVersions()
        {
            TestHelper.OnlyOnce( () =>
            {
                TestHelper.EnsureDatabase( _db, reset: true ).Should().BeTrue();
                _manager.OpenFromConnectionString( TestHelper.GetConnectionString( _db.DatabaseName ) ).Should().BeTrue();
                // Triggers a Initialize. Required for tests
                // that start with a write to the same database.
                _reader.GetOriginalVersions( TestHelper.Monitor );
            } );
            if( _manager.Connection == null )
            {
                _manager.OpenFromConnectionString( TestHelper.GetConnectionString( _db.DatabaseName ) ).Should().BeTrue();
            }
            _manager.ExecuteOneScript( "delete from CKCore.tItemVersionStore" );
        }

        [TearDown]
        public void CloseConnection()
        {
            _manager.Close();
        }

        [Test]
        public void downgrading_version_is_ignored_and_an_error_is_logged()
        {
            var oVersions = new VersionedTypedName[]
            {
                new VersionedTypedName( "A", "T1", new Version(1,0,0) ),
                new VersionedTypedName( "B", "T2", new Version(1,1,1) )
            };
            var versions = oVersions.Select( v => new VersionedNameTracked( v ) ).ToArray();
            versions[0].SetNewVersion( new Version( 1, 0, 1 ), "T1Bis" );
            versions[1].SetNewVersion( new Version( 1, 1, 2 ), "T2Bis" );
            _writer.SetVersions( TestHelper.Monitor, _reader, versions, true );
            CheckVersions( "A - 1.0.1 - T1Bis, B - 1.1.2 - T2Bis" );

            versions = oVersions.Select( v => new VersionedNameTracked( v ) ).ToArray();
            versions[0].SetNewVersion( new Version( 1, 0, 0 ), "ChangingType" );
            versions[1].SetNewVersion( new Version( 1, 1, 2 ), "T2Bis" );
            bool hasLoggedError = false;
            using( TestHelper.Monitor.OnError( () => hasLoggedError = true ) )
            {
                _writer.SetVersions( TestHelper.Monitor, _reader, versions, true );
            }
            hasLoggedError.Should().BeTrue();
            CheckVersions( "A - 1.0.1 - T1Bis, B - 1.1.2 - T2Bis" );

            versions = oVersions.Select( v => new VersionedNameTracked( v ) ).ToArray();
            versions[0].SetNewVersion( new Version( 1, 0, 1 ), "T1Bis" );
            versions[1].SetNewVersion( new Version( 1, 0, 0 ), "VersionRegression" );
            hasLoggedError = false;
            using( TestHelper.Monitor.OnError( () => hasLoggedError = true ) )
            {
                _writer.SetVersions( TestHelper.Monitor, _reader, versions, true );
            }
            hasLoggedError.Should().BeTrue();
            CheckVersions( "A - 1.0.1 - T1Bis, B - 1.1.2 - T2Bis" );
        }

        [Test]
        public void handling_unaccessed_items_on_same_or_different_database()
        {
            var oVersions = new VersionedTypedName[]
            {
                new VersionedTypedName( "A", "T1", new Version(1,0,0) ),
                new VersionedTypedName( "B", "T2", new Version(1,1,1) )
            };
            var versions = oVersions.Select( v => new VersionedNameTracked( v ) ).ToArray();

            // Since we claim to be on the same database, the table does not need to be updated:
            // Versions do not appear because they are already here.
            _writer.SetVersions( TestHelper.Monitor, _reader, versions, deleteUnaccessedItems: false );

            CheckVersions( "" );

            // Here we claim to be on a different database, the table is updated.
            _writer.SetVersions( TestHelper.Monitor, null, versions, deleteUnaccessedItems: false );

            CheckVersions( "A - 1.0.0 - T1, B - 1.1.1 - T2" );

            versions[1].Accessed = true;
            // On the same database, A is removed.
            _writer.SetVersions( TestHelper.Monitor, _reader, versions, deleteUnaccessedItems: true );

            CheckVersions( "B - 1.1.1 - T2" );

            versions[1].Accessed = false;
            // On a different database, unaccessed items are removed too.
            _writer.SetVersions( TestHelper.Monitor, null, versions, deleteUnaccessedItems: true );

            CheckVersions( "" );
        }

        void CheckVersions( string versions )
        {
            IEnumerable<VersionedTypedName> back = _reader.GetOriginalVersions( TestHelper.Monitor );
            back.OrderBy( v => v.FullName )
                .Select( v => v.ToString() )
                .Concatenate()
                .Should().Be( versions );
        }

    }
}
