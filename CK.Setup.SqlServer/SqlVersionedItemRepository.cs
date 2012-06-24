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
        readonly SqlManager _manager;
        SqlCommand _get;
        SqlCommand _set;
        SqlCommand _del;
        bool _initialized;

        public SqlVersionedItemRepository( SqlManager manager )
        {
            if( manager == null ) throw new ArgumentNullException( "manager" );
            _manager = manager;
        }

        public VersionedName GetCurrent( IVersionedItem i )
        {
            if( !_initialized ) AutoInitialize();
            if( _get == null )
            {
                _get = new SqlCommand( "CKCore.sItemVersionGet" );
                _get.CommandType = CommandType.StoredProcedure;
                _get.Parameters.Add( "@ItemType", SqlDbType.VarChar, 16 );
                _get.Parameters.Add( "@FullName", SqlDbType.NVarChar, 128 );
                _get.Parameters.Add( "@ItemVersion", SqlDbType.VarChar, 32 ).Direction = ParameterDirection.Output;
            }
            _get.Parameters[0].Value = i.ItemType;
            _get.Parameters[1].Value = i.FullName;
            _manager.Connection.ExecuteNonQuery( _get );
            Version v;
            if( _get.Parameters[2].Value != DBNull.Value && Version.TryParse( (string)_get.Parameters[2].Value, out v ) )
            {
                return new VersionedName( i.FullName, v );
            }
            return null;
        }

        public void SetCurrent( IVersionedItem i )
        {
            if( !_initialized ) AutoInitialize();
            if( _set == null )
            {
                _set = new SqlCommand( "CKCore.sItemVersionSet" );
                _set.CommandType = CommandType.StoredProcedure;
                _set.Parameters.Add( "@ItemType", SqlDbType.VarChar, 16 );
                _set.Parameters.Add( "@FullName", SqlDbType.NVarChar, 128 );
                _set.Parameters.Add( "@ItemVersion", SqlDbType.VarChar, 32 );
            }
            _set.Parameters[0].Value = i.ItemType;
            _set.Parameters[1].Value = i.FullName;
            _set.Parameters[2].Value = i.Version != null ? i.Version.ToString() : String.Empty;
            _manager.Connection.ExecuteNonQuery( _set );
        }

        public void Delete( string fullName )
        {
            if( !_initialized ) AutoInitialize();
            if( _del == null )
            {
                _del = new SqlCommand( "CKCore.sItemVersionDelete" );
                _del.CommandType = CommandType.StoredProcedure;
                _del.Parameters.Add( "@FullName", SqlDbType.NVarChar, 128 );
            }
            _del.Parameters[0].Value = fullName;
            _manager.Connection.ExecuteNonQuery( _del );
        }

        public void Dispose()
        {
            if( _get != null ) _get.Dispose();
            if( _set != null ) _set.Dispose();
            if( _del != null ) _del.Dispose();
            _get = _set = _del = null;
        }

        void AutoInitialize()
        {
            using( _manager.Logger.Filter( LogLevelFilter.Error ) )
            {
                if( !_manager.EnsureCKCoreIsInstalled( _manager.Logger )
                    || !_manager.ExecuteScripts( SqlHelper.SplitGoSeparator( _script ), _manager.Logger ) )
                {
                    throw new Exception( "Unable to initialize SqlVersionedItemRepository." );
                }
            }
            _initialized = true;
        }

        static string _script = @"
if object_id('CKCore.tItemVersion') is null
begin
	create table CKCore.tItemVersion
	(
		ItemType varchar(16) collate Latin1_General_BIN not null,
		FullName nvarchar(128) collate Latin1_General_BIN not null,
		ItemVersion varchar(32) not null,
		constraint UK_tItemVersion unique (ItemType, FullName)
	);
end
GO
if object_id('CKCore.sItemVersionGet') is not null drop procedure CKCore.sItemVersionGet;
GO
create procedure CKCore.sItemVersionGet
	(
		@ItemType		varchar(16),
		@FullName		nvarchar(128),
		@ItemVersion	varchar(32) output
	)
as
begin
	set @ItemVersion = null;
	set @ItemType = Upper(@ItemType);
	if @ItemType = 'TABLE' or @ItemType = 'PROCEDURE' or @ItemType = 'FUNCTION'  or @ItemType = 'VIEW' 
	begin
		select @ItemVersion = cast(value as varchar(32))
			from ::fn_listextendedproperty( 'CKVersion', 'schema', PARSENAME(@FullName,2), @ItemType, PARSENAME(@FullName,1), NULL, NULL);
	end
	else
	begin
		select @ItemVersion = t.ItemVersion
			from CKCore.tItemVersion t
			where t.FullName = @FullName and t.ItemType = @ItemType;
	end
	return 0;
end
GO
if object_id('CKCore.sItemVersionSet') is not null drop procedure CKCore.sItemVersionSet;
GO
create procedure CKCore.sItemVersionSet
	(
		@ItemType		varchar(16),
		@FullName		nvarchar(128),
		@ItemVersion	varchar(32)
	)
AS
begin
	set nocount on declare @ok int, @ret int, @rc int set @ok = 0 begin tran --[!beginsp]
	set @ItemType = Upper(@ItemType);
	if @ItemType = 'PROCEDURE' or @ItemType = 'FUNCTION' or @ItemType = 'TABLE' or @ItemType = 'VIEW'
	begin
		declare @Schema sysname = PARSENAME(@FullName,2);
		declare @Object sysname = PARSENAME(@FullName,1);
		if exists( select 1 from ::fn_listextendedproperty('CKVersion', 'schema', @Schema, @ItemType, @Object, NULL, NULL) )
		begin
			exec @ret = sp_updateextendedproperty 'CKVersion', @ItemVersion, 'schema', @Schema, @ItemType, @Object;
			set @ok = @@Error if @ok <> 0 goto ErCall if @ret <> 0 goto Error --[!call]
		end 
		else
		begin
			exec @ret = sp_addextendedproperty 'CKVersion', @ItemVersion, 'schema', @Schema, @ItemType, @Object;
			set @ok = @@Error if @ok <> 0 goto ErCall if @ret <> 0 goto Error --[!call]
		end
	end
	else
	begin
		merge CKCore.tItemVersion as target
			using (select ItemType = @ItemType, FullName = @FullName) as source
			on target.ItemType = source.ItemType and target.FullName = source.FullName
			when matched then update set ItemVersion = @ItemVersion
			when not matched then insert (ItemType, FullName, ItemVersion) VALUES (@ItemType, @FullName, @ItemVersion); 
		set @ok = @@Error if @ok <> 0 goto Error --[!catch]			
	end
	StdExit: if @@TranCount > 0 commit return 0 ErCall: raiserror( 'Sub call failed', 16, 1 ) Error: if @@TranCount > 1 commit else if @@TranCount = 1 rollback return -1 --[!endsp]
end
GO
if object_id('CKCore.sItemVersionDelete') is not null drop procedure CKCore.sItemVersionDelete;
GO
create procedure CKCore.sItemVersionDelete
	(
		@FullName nvarchar(128)
	)
AS
begin
	set nocount on;
	delete CKCore.tItemVersion where FullName = @FullName;
	if @@ROWCOUNT = 0 
	begin
		declare @Schema sysname = PARSENAME(@FullName,2);
		declare @Object sysname = PARSENAME(@FullName,1);
		declare @ret int;
		if exists( select * from ::fn_listextendedproperty('CKVersion', 'schema', @Schema, 'PROCEDURE', @Object, NULL, NULL) )
		begin
			exec @ret = sp_dropextendedproperty 'CKVersion', 'schema', @Schema, 'PROCEDURE', @Object;
			return @ret;
		end 
		else if exists( select * from ::fn_listextendedproperty('CKVersion', 'schema', @Schema, 'FUNCTION', @Object, NULL, NULL) )
		begin
			exec @ret = sp_dropextendedproperty 'CKVersion', 'schema', @Schema, 'FUNCTION', @Object;
			return @ret;
		end 
		else if exists( select * from ::fn_listextendedproperty('CKVersion', 'schema', @Schema, 'VIEW', @Object, NULL, NULL) )
		begin
			exec @ret = sp_dropextendedproperty 'CKVersion', 'schema', @Schema, 'VIEW', @Object;
			return @ret;
		end 
		else if exists( select * from ::fn_listextendedproperty('CKVersion', 'schema', @Schema, 'TABLE', @Object, NULL, NULL) )
		begin
			exec @ret = sp_dropextendedproperty 'CKVersion', 'schema', @Schema, 'TABLE', @Object;
			return @ret;
		end 
	end
	else
	begin
		return @@Error;
	end
end
GO
declare @ThisVersion varchar(32) = '1.0.1'
exec CKCore.sItemVersionSet 'Package', 'CK.SqlVersionedItemRepository', @ThisVersion;
exec CKCore.sItemVersionSet 'Table', 'CKCore.tItemVersion', @ThisVersion;
exec CKCore.sItemVersionSet 'Procedure', 'CKCore.sItemVersionSet', @ThisVersion;
exec CKCore.sItemVersionSet 'Procedure', 'CKCore.sItemVersionGet', @ThisVersion;
exec CKCore.sItemVersionSet 'Procedure', 'CKCore.sItemVersionDelete', @ThisVersion;
";

    }
}
