using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CK.Core;
using CK.Setup;
using System.Diagnostics;

namespace CK.SqlServer.Setup
{
    public class SqlVersionedItemReader : IVersionedItemReader
    {
        /// <summary>
        /// Gets the current version of this store.
        /// </summary>
        public static int CurrentVersion => _upgradeScripts.Length;

        bool _initialized;

        public SqlVersionedItemReader( ISqlManager manager )
        {
            if( manager == null ) throw new ArgumentNullException( "manager" );
            Manager = manager;
        }

        internal readonly ISqlManager Manager;

        void AutoInitialize()
        {
            Debug.Assert( !_initialized );
            var monitor = Manager.Monitor;
            using( monitor.OpenTrace().Send( "Installing SqlVersionedItemRepository store." ) )
            {
                int ver = (int)Manager.ExecuteScalar( _scriptCreateAndGetVersion );

                if( ver == CurrentVersion )
                {
                    monitor.CloseGroup( $"Already installed in version {CurrentVersion}." );
                }
                else 
                {
                    if( ver == -1 )
                    {
                        monitor.CloseGroup( $"Installed first store version." );
                        ver = 0;
                    }
                    while( ver < CurrentVersion )
                    {
                        using( monitor.OpenInfo().Send( $"Upgrading to Version = {ver}." ) )
                        {
                            Manager.ExecuteNonQuery( _upgradeScripts[ver++] );
                        }
                    }
                    Manager.ExecuteNonQuery( $"update CKCore.tItemVersionStore set ItemVersion = '{CurrentVersion}' where FullName = N'CK.SqlVersionedItemRepository';" );
                }
                _initialized = true;
            }
        }

        public IEnumerable<VersionedTypedName> GetOriginalVersions( IActivityMonitor monitor )
        {
            if( !_initialized ) AutoInitialize();
            using( var c = new SqlCommand( "select FullName, ItemType, ItemVersion from CKCore.tItemVersionStore where FullName <> N'CK.SqlVersionedItemRepository'" ) { Connection = Manager.Connection.Connection } )
            using( var r = c.ExecuteReader() )
            {
                while( r.Read() )
                {
                    string fullName = r.GetString( 0 );
                    Version v;
                    if( !Version.TryParse( r.GetString( 2 ), out v ) )
                    {
                        throw new Exception( $"Unable to parse version for {fullName}: '{r.GetString(2)}'." );
                    }
                    yield return new VersionedTypedName( fullName, r.GetString( 1 ), v );
                }
            }
        }

        public VersionedName OnVersionNotFound( IVersionedItem item, Func<string, VersionedTypedName> originalVersions )
        {
            // Maps "Model.XXX" to "XXX" versions for default context and database.
            if( item.FullName.StartsWith( "[]db^Model.", StringComparison.Ordinal ) )
            {
                return originalVersions( "[]db^" + item.FullName.Substring( 11 ) );
            }
            // Old code: Handle non-prefixed FullName when not found.
            return item.FullName.StartsWith( "[]db^", StringComparison.Ordinal ) 
                    ? originalVersions( item.FullName.Substring( 5 ) )
                    : null;
        }

        public VersionedName OnPreviousVersionNotFound( IVersionedItem item, VersionedName prevVersion, Func<string, VersionedTypedName> originalVersions )
        {
            // Maps "Model.XXX" to "XXX" versions for default context and database.
            if( prevVersion.FullName.StartsWith( "[]db^Model.", StringComparison.Ordinal ) )
            {
                return originalVersions( "[]db^" + prevVersion.FullName.Substring( 11 ) );
            }
            // Old code: Handle non-prefixed FullName when not found.
            return prevVersion.FullName.StartsWith( "[]db^", StringComparison.Ordinal )
                    ? originalVersions( prevVersion.FullName.Substring( 5 ) )
                    : null;
        }

        internal static string CreateTemporaryTableScript = @"declare @T table(F nvarchar(400) collate Latin1_General_BIN2 not null,T varchar(16) collate Latin1_General_BIN2 not null,V varchar(32) not null);";

        internal static string MergeTemporaryTableScript = @"
merge CKCore.tItemVersionStore as target
	using( select * from @T ) as source on target.FullName = source.F
	when matched then update set ItemType = source.T, ItemVersion = source.V
	when not matched by target then insert( FullName, ItemType, ItemVersion ) values( source.F, source.T, source.V );";

        static string _scriptCreateAndGetVersion = SqlCKCoreInstaller.EnsureCKCoreSchemaScript + @"
if OBJECT_ID('CKCore.tItemVersionStore') is null
begin
	create table CKCore.tItemVersionStore
	(
		FullName nvarchar(400) collate Latin1_General_BIN2 not null,
		ItemType varchar(16) collate Latin1_General_BIN2 not null,
		ItemVersion varchar(32) not null,
		constraint CKCore_PK_tItemVersionStore primary key(FullName)
	);
	if OBJECT_ID('CKCore.tItemVersion') is not null
	begin
		insert into CKCore.tItemVersionStore( FullName, ItemType, ItemVersion )
			select FullName, ItemType, ItemVersion from CKCore.tItemVersion where FullName <> N'CK.SqlVersionedItemRepository';
		drop table CKCore.tItemVersion;
	end
	insert into CKCore.tItemVersionStore( FullName, ItemType, ItemVersion ) values( N'CK.SqlVersionedItemRepository', '', '0' );
	select -1;
end
else
begin
	select convert( int, ItemVersion) from CKCore.tItemVersionStore where FullName = N'CK.SqlVersionedItemRepository';
end";

        const string _update1 = @"update CKCore.tItemVersionStore set FullName = stuff(FullName,6,8,'Model.') where FullName like '[[]]db^Objects.%'";

        readonly static string[] _upgradeScripts = new[] { _update1 };

    }
}
