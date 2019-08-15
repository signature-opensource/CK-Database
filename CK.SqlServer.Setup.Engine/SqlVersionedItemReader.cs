using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CK.Core;
using CK.Setup;
using CSemVer;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implements <see cref="IVersionedItemReader"/> on a Sql Server database. 
    /// </summary>
    public class SqlVersionedItemReader : IVersionedItemReader
    {
        /// <summary>
        /// Gets the current version of this store.
        /// </summary>
        public static int CurrentVersion => _upgradeScripts.Length;

        bool _initialized;

        /// <summary>
        /// Initializes a new <see cref="SqlVersionedItemReader"/>.
        /// </summary>
        /// <param name="manager">The sql manager to use.</param>
        public SqlVersionedItemReader( ISqlManager manager )
        {
            if( manager == null ) throw new ArgumentNullException( "manager" );
            Manager = manager;
        }

        internal readonly ISqlManager Manager;

        /// <summary>
        /// Initializes the tables and objects required to support item versioning.
        /// This is public since other participants may way want to setup this sub system.
        /// </summary>
        /// <param name="m">The sql manager to use.</param>
        public static void AutoInitialize( ISqlManager m )
        {
            var monitor = m.Monitor;
            using( monitor.OpenTrace( "Installing SqlVersionedItemRepository store." ) )
            {
                int ver = (int)(m.ExecuteScalar( _scriptCreateAndGetVersion ) ?? -1);

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
                        using( monitor.OpenInfo( $"Upgrading to Version = {ver}." ) )
                        {
                            m.ExecuteNonQuery( _upgradeScripts[ver++] );
                        }
                    }
                    m.ExecuteNonQuery( $"update CKCore.tItemVersionStore set ItemVersion = '{CurrentVersion}' where FullName = N'CK.SqlVersionedItemRepository';" );
                }
            }
        }

        /// <summary>
        /// Gets the versions stored in the database.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>The set of original verions.</returns>
        public OriginalReadInfo GetOriginalVersions( IActivityMonitor monitor )
        {
            var result = new List<VersionedTypedName>();
            var fResult = new List<VFeature>();
            if( !_initialized )
            {
                AutoInitialize( Manager );
                _initialized = true;
            }
            using( var c = new SqlCommand( "select FullName, ItemType, ItemVersion from CKCore.tItemVersionStore where FullName <> N'CK.SqlVersionedItemRepository'" ) { Connection = Manager.Connection } )
            using( var r = c.ExecuteReader() )
            {
                while( r.Read() )
                {
                    string fullName = r.GetString( 0 );
                    string itemType = r.GetString( 1 );
                    if( itemType == "VFeature" )
                    {
                        fResult.Add( new VFeature( fullName, SVersion.Parse( r.GetString( 2 ) ) ) );
                    }
                    else
                    {
                        Version v;
                        if( !Version.TryParse( r.GetString( 2 ), out v ) )
                        {
                            throw new Exception( $"Unable to parse version for {fullName}: '{r.GetString( 2 )}'." );
                        }
                        result.Add( new VersionedTypedName( fullName, r.GetString( 1 ), v ) );
                    }
                }
            }
            return new OriginalReadInfo( result, fResult );
        }

        /// <summary>
        /// Called by the engine when the version is not found for the item before using the <see cref="IVersionedItem.PreviousNames"/>.
        /// This is a "first chance" optional hook.
        /// This enables any possible mapping and fallback to take place.
        /// </summary>
        /// <param name="item">The item for which no direct version has been found.</param>
        /// <param name="originalVersions">
        /// A getter for original versions. This can help the implementation to avoid duplicating its own version
        /// of <see cref="GetOriginalVersions"/>.
        /// </param>
        /// <returns>A <see cref="VersionedName"/> with the mapped name or null if not found.</returns>
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

        /// <summary>
        /// Called by the engine when a previous version is not found for the item
        /// This is an optional hook.
        /// </summary>
        /// <param name="item">Item for which a version should be found.</param>
        /// <param name="prevVersion">The not found previous version.</param>
        /// <param name="originalVersions">
        /// A getter for original versions. This can help the implementation to avoid duplicating its own version
        /// of <see cref="GetOriginalVersions"/>.
        /// </param>
        /// <returns>A <see cref="VersionedName"/> with the mapped name or null if not found.</returns>
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
