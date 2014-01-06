using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlVersionedItemRepository : IVersionedItemRepository, IDisposable
    {
        public readonly static Version CurrentVersion = new Version( 4, 1, 6, 1142 );

        readonly SqlManager _manager;
        SqlCommand _get;
        SqlCommand _set;
        bool _initialized;

        public SqlVersionedItemRepository( SqlManager manager )
        {
            if( manager == null ) throw new ArgumentNullException( "manager" );
            _manager = manager;
        }

        public VersionedName GetCurrent( IVersionedItem i )
        {
            if( !_initialized ) AutoInitialize();
            string t = CheckItemType( i );
            if( _get == null )
            {
                _get = new SqlCommand( "CKCore.sItemVersionGet" );
                _get.CommandType = CommandType.StoredProcedure;
                _get.Parameters.Add( "@ItemType", SqlDbType.VarChar, 16 );
                _get.Parameters.Add( "@FullName", SqlDbType.NVarChar, 128 );
                _get.Parameters.Add( "@ItemVersion", SqlDbType.VarChar, 32 ).Direction = ParameterDirection.Output;
            }
            _get.Parameters[0].Value = t;

            VersionedName n = DoGetVersion( i.FullName );
            // Temporary: Looks up for non-prefixed FullName when not found and cleans up the db.
            if( n == null && i.FullName.StartsWith( "[]db^", StringComparison.Ordinal ) )
            {
                n = DoGetVersion( i.FullName.Substring( 5 ) );
                if( n != null )
                {
                    DoSetCurrent( t, i.FullName, n.Version );
                    Delete( i.FullName.Substring( 5 ) );
                }
            }
            // Uses Previous Names if any.
            if( n == null )
            {
                var prev = i.PreviousNames;
                if( prev != null )
                {
                    // Should be replaced with a CodeContract on IVersionedItem...
                    if( !prev.IsSortedStrict( ( v1, v2 ) => v1.Version.CompareTo( v2.Version ) ) )
                    {
                        throw new CKException( "PreviousNames must be ordered by their Version for FullName='{0}'", i.FullName );
                    }
                    var orderedPrev = prev.Reverse();
                    foreach( var prevVersion in orderedPrev )
                    {
                        n = DoGetVersion( prevVersion.FullName );
                        if( n != null ) break;
                    }
                }
            }
            return n;
        }

        private VersionedName DoGetVersion( string fullName )
        {
            _get.Parameters[1].Value = fullName;
            _manager.Connection.ExecuteNonQuery( _get );
            Version v;
            if( _get.Parameters[2].Value != DBNull.Value && Version.TryParse( (string)_get.Parameters[2].Value, out v ) )
            {
                return new VersionedName( fullName, v );
            }
            return null;
        }

        public void SetCurrent( IVersionedItem i )
        {
            if( !_initialized ) AutoInitialize();
            DoSetCurrent( CheckItemType( i ), i.FullName, i.Version );
        }

        private void DoSetCurrent( string t, string n, Version v )
        {
            if( _set == null )
            {
                _set = new SqlCommand( "CKCore.sItemVersionSet" );
                _set.CommandType = CommandType.StoredProcedure;
                _set.Parameters.Add( "@ItemType", SqlDbType.VarChar, 16 );
                _set.Parameters.Add( "@FullName", SqlDbType.NVarChar, 128 );
                _set.Parameters.Add( "@ItemVersion", SqlDbType.VarChar, 32 );
            }
            _set.Parameters[0].Value = t;
            _set.Parameters[1].Value = n;
            if( v != null ) _set.Parameters[2].Value = v.ToString();
            else _set.Parameters[2].Value = DBNull.Value;
            _manager.Connection.ExecuteNonQuery( _set );
        }

        private static string CheckItemType( IVersionedItem i )
        {
            string type = i.ItemType;
            if( String.IsNullOrEmpty( type ) ) throw new ArgumentOutOfRangeException( "ItemType", type, "IVersionedItem.ItemType must be not null nor empty." );
            type = type.Trim();
            if( type.Length == 0 || type.Length > 16 ) throw new ArgumentOutOfRangeException( "ItemType", type, "IVersionedItem.ItemType must be between 1 and 16 characters long." );
            return type;
        }

        public void Delete( string fullName )
        {
            if( !_initialized ) AutoInitialize();
            DoSetCurrent( String.Empty, fullName, null );
        }

        public void Dispose()
        {
            if( _get != null ) _get.Dispose();
            if( _set != null ) _set.Dispose();
            _get = _set = null;
        }

        void AutoInitialize()
        {
            var logger = _manager.Logger;
            using( logger.OpenGroup( LogLevel.Trace, "Installing SqlVersionedItemRepository." ) )
            {
                if( !_manager.EnsureCKCoreIsInstalled( _manager.Logger ) ) throw new Exception( "Unable to initialize CKCore." );
                Version v = GetVersion();
                if( v == CurrentVersion )
                {
                    logger.CloseGroup( String.Format( "Already installed in version {0}.", CurrentVersion ) );
                }
                else 
                {
                    if( v == Util.EmptyVersion )
                    {
                        logger.Info( "Installing current version {0}.", CurrentVersion );
                    }
                    else
                    {
                        logger.Info( "Updgrading from {0} to {1}.", v, CurrentVersion );
                    }
                    using( logger.Filter( LogLevelFilter.Error ) )
                    {
                        if( v == Util.EmptyVersion )
                        {
                            ExecScript( logger, _scriptCreate );
                        }
                        else if( v.Major == 2 && v.Minor == 6 )
                        {
                            ExecScript( logger, _scriptFrom2_6_27 );
                        }
                        ExecScript( logger, _scriptAlways.Replace( "$Ver$", CurrentVersion.ToString() ) );
                    }
                }
                _initialized = true;
            }
        }

        private void ExecScript( IActivityLogger logger, string s )
        {
            var p = new SimpleScriptTagHandler( s );
            if( !p.Expand( logger, false ) ) throw new Exception( "Script error." );
            if( !_manager.ExecuteScripts( p.SplitScript().Select( script => script.Body ), _manager.Logger ) )
            {
                throw new Exception( "Unable to initialize SqlVersionedItemRepository." );
            }
        }

        Version GetVersion()
        {
            Version v;
            string ver = (string)_manager.Connection.ExecuteScalar( "declare @V varchar(32)='';if object_id('CKCore.sItemVersionGet') is not null exec CKCore.sItemVersionGet 'Package', 'CK.SqlVersionedItemRepository', @V output;select coalesce(@V,'');" );
            if( ver == null || !Version.TryParse( ver, out v ) ) v = Util.EmptyVersion;
            return v;
        }

        static string _scriptCreate = @"
create table CKCore.tItemVersion
(
	FullName nvarchar(128) collate Latin1_General_BIN not null,
	ItemType varchar(16) collate Latin1_General_BIN not null,
	ItemVersion varchar(32) not null,
	constraint PK_tItemVersion primary key(FullName)
);
";
        static string _scriptFrom2_6_27 = @"
alter table CKCore.tItemVersion drop UK_tItemVersion;
alter table CKCore.tItemVersion add
	constraint PK_tItemVersion primary key(FullName);
";
        static string _scriptAlways = @"
if object_id('CKCore.sItemVersionGet') is not null drop procedure CKCore.sItemVersionGet;
if object_id('CKCore.sItemVersionSet') is not null drop procedure CKCore.sItemVersionSet;
if object_id('CKCore.sItemVersionDelete') is not null drop procedure CKCore.sItemVersionDelete;
if object_id('CKCore.sItemExtractLocationAndName') is not null drop procedure CKCore.sItemExtractLocationAndName;

GO
-- Extracting location from FullName (extracts 'loc' and 'name' from []loc^name). 
-- Null if no location can be found.
--
-- Usage:
--   declare @location varchar(128), @objectName varchar(128);
--   exec CKCore.sItemExtractLocationAndName '[Context]theLoc^the.name', @location output, @objectName output;
--   select @location, @objectName;
--
create procedure CKCore.sItemExtractLocationAndName
( 
	@FullName varchar(128), 
	@Location varchar(128) output,
	@ObjectName varchar(128) output
)
as begin
	declare @i2 int = CHARINDEX('^',@FullName,1);
	declare @i1 int = CHARINDEX(']',@FullName,1);
	if @i1 < @i2 
	begin
		set @Location = SUBSTRING(@FullName,@i1+1,@i2-@i1-1);
		set @ObjectName = SUBSTRING(@FullName,@i2+1,128);
	end
	else 
	begin
		set @Location = null;
		set @ObjectName = null;
	end
end
GO
-- When @ItemVersion is null, the version information is deleted from tItemVersion table.
-- This procedure inserts or updates the version even if we may detect that the object does not 
-- exist (see sItemVersionGet). This is to enable setting the version of a database object before
-- actually creating it.
create procedure CKCore.sItemVersionSet
	(
		@ItemType		varchar(16),
		@FullName		nvarchar(128),
		@ItemVersion	varchar(32)
	)
as
begin
    set nocount on;
    if @ItemVersion is null 
    begin
	    delete CKCore.tItemVersion where FullName = @FullName;
    end
    else
    begin
        set @ItemType = Upper(@ItemType);
        merge CKCore.tItemVersion as t
		    using (select FullName = @FullName) as s
		    on t.FullName = s.FullName
		    when matched then update set ItemVersion = @ItemVersion, ItemType = @ItemType
		    when not matched then insert (ItemType, FullName, ItemVersion) values (@ItemType, @FullName, @ItemVersion); 
    end
    return 0;
end
GO
-- Gets version information for an item. If the object corresponds to a real sql object (procedure, function, table, view) AND the 
-- location in the @FullName is this database ('db'), this procedure checks the existence of the object and if it can not be found, 
-- the item is deleted from the tItemVersion table.
-- Usage:
--    declare @itemVersion varchar(32);
--    exec CKCore.sItemVersionGet 'PACKAGE', 'CK.SqlVersionedItemRepository', @itemVersion output;
--    select @itemVersion;
create procedure CKCore.sItemVersionGet
	(
		@ItemType		varchar(16),
		@FullName		nvarchar(128),
		@ItemVersion	varchar(32) output
	)
as
begin
    set nocount on;
	set @ItemVersion = null;
	set @ItemType = Upper(@ItemType);
	select @ItemVersion = t.ItemVersion from CKCore.tItemVersion t where t.FullName = @FullName;
		
	-- Temporary: lookup version on extended properties for default context and database.
	if @@ROWCOUNT = 0 
		and (@ItemType = 'TABLE' or @ItemType = 'PROCEDURE' or @ItemType = 'FUNCTION'  or @ItemType = 'VIEW')
		and SUBSTRING(@FullName,1,5)='[]db^'  
	begin
		declare @oldFName varchar(128) = SUBSTRING(@FullName,6,128);
		select @ItemVersion = cast(value as varchar(32))
			from sys.fn_listextendedproperty( 'CKVersion', 'schema', PARSENAME(@oldFName,2), @ItemType, PARSENAME(@oldFName,1), NULL, NULL);
	end
	-- /Temporary

	if @ItemVersion is not null
		and
		(@ItemType = 'TABLE' or @ItemType = 'PROCEDURE' or @ItemType = 'FUNCTION'  or @ItemType = 'VIEW')
	begin
		-- Extracting location from FullName.
		declare @location varchar(128), @objectName varchar(128);
		exec CKCore.sItemExtractLocationAndName @FullName, @location output, @objectName output;
		if @location in ( 'db' )
		begin
			if OBJECT_ID( @objectName ) is null
			begin
				delete from CKCore.tItemVersion where FullName = @FullName;
				set @ItemVersion = null;
			end
		end
	end	
	return 0;
end
GO
exec CKCore.sItemVersionSet 'Package', 'CK.SqlVersionedItemRepository', '$Ver$';
";

    }
}
