using System;
using System.Linq;
using CK.Core;

namespace CK.SqlServer.Setup
{
    internal class SqlCKCoreInstaller
    {
        public readonly static short CurrentVersion = 16;

        /// <summary>
        /// Installs the kernel.
        /// </summary>
        /// <param name="manager">The manager that will be used.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="forceInstall">True to force the installation even if Ver column of CKCore.tSystem where Id = 1 is the same as <see cref="CurrentVersion"/>.</param>
        /// <returns>True on success.</returns>
        public static bool Install( SqlManager manager, IActivityMonitor monitor, bool forceInstall = false )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );

            using( monitor.OpenTrace( $"Installing CKCore kernel (v{CurrentVersion})." ) )
            {
                short ver = 0;
                if( !forceInstall && (ver = (short)manager.ExecuteScalar( "if object_id('CKCore.tSystem') is not null select Ver from CKCore.tSystem where Id=1 else select cast(0 as smallint);" )) == CurrentVersion )
                {
                    monitor.CloseGroup( "Already installed." );
                }
                else
                {
                    monitor.MinimalFilter = LogFilter.Terse;
                    SimpleScriptTagHandler s = new SimpleScriptTagHandler( _script.Replace( "$Ver$", CurrentVersion.ToString() ) );
                    if( !s.Expand( monitor, false ) ) return false;
                    if( !manager.ExecuteScripts( s.SplitScript().Select( one => one.Body ), monitor ) ) return false;
                    if( ver == 0 ) monitor.CloseGroup( String.Format( "Installed in version {0}.", CurrentVersion ) );
                    else monitor.CloseGroup( String.Format( "Installed in version {0} (was {1}).", CurrentVersion, ver ) );
                }
            }
            return true;
        }

        /// <summary>
        /// Exposes the script fragment that tests for CKCore schema and creates it if needed.
        /// </summary>
        public static readonly string EnsureCKCoreSchemaScript = @"
if not exists(select 1 from sys.schemas where name = 'CKCore')
begin
    exec( 'create schema CKCore' );
end
";

        static readonly string _script = EnsureCKCoreSchemaScript + @"
else
begin
    if object_id('CKCore.sErrorRethrow') is not null drop procedure CKCore.sErrorRethrow;
    if object_id('CKCore.sSchemaDropAllConstraints') is not null drop procedure CKCore.sSchemaDropAllConstraints;
    if object_id('CKCore.sSchemaDropAllObjects') is not null drop procedure CKCore.sSchemaDropAllObjects;

    if object_id('CKCore.vForeignKeyDetail') is not null drop view CKCore.vForeignKeyDetail;
    if object_id('CKCore.vConstraintColumns') is not null drop view CKCore.vConstraintColumns;

    if object_id('CKCore.sInvariantRegister') is not null drop procedure CKCore.sInvariantRegister;
    if object_id('CKCore.sInvariantRun') is not null drop procedure CKCore.sInvariantRun;
    if object_id('CKCore.sInvariantRunAll') is not null drop procedure CKCore.sInvariantRunAll;
    if object_id('CKCore.sRefBazookation') is not null drop procedure CKCore.sRefBazookation;
end
GO
create procedure CKCore.sErrorRethrow
(
	@ProcId int
)
as
begin
	declare @EM nvarchar(2048) = ERROR_MESSAGE();
	if @EM is not null
	begin
		declare @ON sysname;
		if @EM like 'ck:%-{%}-[[]%]%' 
		begin
			set @ON = OBJECT_NAME(@ProcId);
			if @ON is null set @ON = N'(dynamic)';
			else set @ON = OBJECT_SCHEMA_NAME(@ProcId)+'.'+@ON;
			set @EM = @EM+'<-'+@ON;
			raiserror( @EM, 16, 1 );
		end
		else
		begin
			set @ON = ERROR_PROCEDURE();
			if @ON is null set @ON = N'(dynamic)';
			declare @LN int = ERROR_LINE();
			raiserror( N'ck:%s-{%d}-[%s]', 16, 1, @ON, @LN, @EM );
		end			
	end
end
GO
create procedure CKCore.sSchemaDropAllConstraints
	@SchemaName sysname
as
begin
	declare @C cursor;
	set @C = cursor local read_only for 
		select TABLE_NAME, CONSTRAINT_NAME 
		from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
		where TABLE_SCHEMA = @SchemaName
				and CONSTRAINT_TYPE='FOREIGN KEY';
	declare @tName sysname;
	declare @cName sysname;
	open @C;
	fetch from @C into @tName, @cName
	while @@FETCH_STATUS = 0
	begin
		declare @cmd nvarchar(800);
		set @cmd = N'alter table '+QUOTENAME(@SchemaName)+'.'+QUOTENAME(@tName)+N' drop constraint '+@cName;
		exec sp_executesql @cmd;
        fetch next from @C into @tName, @cName;
	end 
end
GO
create procedure CKCore.sSchemaDropAllObjects
	@SchemaName sysname,
	@ObjectType sysname = null
as
begin
	set @ObjectType = Upper(@ObjectType);
	declare @DeleteCount int;
	declare @DependencyError int;
runIt:
	set @DeleteCount = 0;
	set @DependencyError = 0;
	declare @tName sysname;
	-- Pre Sql 2012: raiserror again.
	declare
		@ErrorMessage nvarchar(4000),
		@ErrorNumber int,
		@ErrorSeverity int,
		@ErrorState int,
		@ErrorLine int,
		@ErrorProcedure nvarchar(200);
    declare @cmd nvarchar(4000);
	if @ObjectType is null or @ObjectType = 'CONSTRAINT'
	begin
		declare @C1 cursor;
		set @C1 = cursor local read_only for 
			select TABLE_NAME, CONSTRAINT_NAME 
			from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
			where TABLE_SCHEMA = @SchemaName
					and CONSTRAINT_TYPE='FOREIGN KEY';
		declare @cName sysname;
		open @C1;
		fetch from @C1 into @tName, @cName
		while @@FETCH_STATUS = 0
		begin
			set @cmd = N'alter table '+QUOTENAME(@SchemaName)+'.'+QUOTENAME(@tName)+N' drop constraint '+QUOTENAME(@cName);
			begin try
				exec sp_executesql @cmd;
				set @DeleteCount = @DeleteCount + 1;
			end try
			begin catch
				select
					@ErrorMessage = ERROR_MESSAGE(),
					@ErrorNumber = ERROR_NUMBER(),
					@ErrorSeverity = ERROR_SEVERITY(),
					@ErrorState = ERROR_STATE(),
					@ErrorLine = ERROR_LINE(),
					@ErrorProcedure = ISNULL(ERROR_PROCEDURE(), '-');
				if @ErrorNumber = 3729
				begin
					set @DependencyError = @DependencyError + 1;
				end
				else
				begin
					select @ErrorMessage = N'Error %d, Level %d, State %d, Procedure %s, Line %d, ' + 'Message: ' + @ErrorMessage;
					raiserror( @ErrorMessage, @ErrorSeverity, 1, @ErrorNumber, @ErrorSeverity, @ErrorState, @ErrorProcedure, @ErrorLine )
				end
			end catch
			fetch next from @C1 into @tName, @cName;
		end 
    end
	if @ObjectType is null or @ObjectType = 'PROCEDURE' or @ObjectType = 'FUNCTION'
	begin
		declare @C2 cursor
		set @C2 = cursor local read_only for
			select ROUTINE_NAME, ROUTINE_TYPE
			from INFORMATION_SCHEMA.ROUTINES
			where ROUTINE_SCHEMA = @SchemaName 
					and ROUTINE_TYPE=IsNull(@ObjectType,ROUTINE_TYPE)
					and ROUTINE_NAME not like 'sys[_]%'
					and ROUTINE_NAME not like 'dt[_]%'
					and ROUTINE_NAME <> 'sSchemaDropAllConstraints'
					and ROUTINE_NAME <> 'sSchemaDropAllObjects';
		declare @rType nvarchar(20);
		declare @rName sysname;
		open @C2;
		fetch from @C2 into @rName, @rType;
		while @@FETCH_STATUS = 0
		begin
		    set @cmd = N'drop '+@rType+' '+QUOTENAME(@SchemaName)+'.'+QUOTENAME(@rName);
			begin try
				exec sp_executesql @cmd;
				set @DeleteCount = @DeleteCount + 1;
			end try
			begin catch
				select
					@ErrorMessage = ERROR_MESSAGE(),
					@ErrorNumber = ERROR_NUMBER(),
					@ErrorSeverity = ERROR_SEVERITY(),
					@ErrorState = ERROR_STATE(),
					@ErrorLine = ERROR_LINE(),
					@ErrorProcedure = ISNULL(ERROR_PROCEDURE(), '-');
				if @ErrorNumber = 3729
				begin
					set @DependencyError = @DependencyError + 1;
				end
				else
				begin
					select @ErrorMessage = N'Error %d, Level %d, State %d, Procedure %s, Line %d, ' + 'Message: ' + @ErrorMessage;
					raiserror( @ErrorMessage, @ErrorSeverity, 1, @ErrorNumber, @ErrorSeverity, @ErrorState, @ErrorProcedure, @ErrorLine )
				end
			end catch
            fetch next from @C2 into @rName, @rType;
		end 
	end
	if  @ObjectType is null or @ObjectType = 'VIEW'
	begin
		declare @C3 cursor;
		set @C3 = cursor local read_only for 
			select TABLE_NAME 
			from INFORMATION_SCHEMA.VIEWS 
			where TABLE_SCHEMA = @SchemaName 
                    and TABLE_NAME not like 'sys%';
		declare @vName sysname;
		open @C3;
		fetch from @C3 into @vName;
		while @@FETCH_STATUS = 0
		begin
			set @cmd = N'drop view '+QUOTENAME(@SchemaName)+'.'+QUOTENAME(@vName);
			begin try
				exec sp_executesql @cmd;
				set @DeleteCount = @DeleteCount + 1;
			end try
			begin catch
				select
					@ErrorMessage = ERROR_MESSAGE(),
					@ErrorNumber = ERROR_NUMBER(),
					@ErrorSeverity = ERROR_SEVERITY(),
					@ErrorState = ERROR_STATE(),
					@ErrorLine = ERROR_LINE(),
					@ErrorProcedure = ISNULL(ERROR_PROCEDURE(), '-');
				if @ErrorNumber = 3729
				begin
					set @DependencyError = @DependencyError + 1;
				end
				else
				begin
					select @ErrorMessage = N'Error %d, Level %d, State %d, Procedure %s, Line %d, ' + 'Message: ' + @ErrorMessage;
					raiserror( @ErrorMessage, @ErrorSeverity, 1, @ErrorNumber, @ErrorSeverity, @ErrorState, @ErrorProcedure, @ErrorLine )
				end
			end catch
			fetch next from @C3 into @vName;
		end
	end
    if @ObjectType is null or @ObjectType = 'TABLE'
	begin
        -- Turns Sql Server 2016+ versioning off for temporal tables before destroying them.
        -- This uses 'feature detection' instead of database version check.
		if object_id('sys.all_columns') is not null
		begin
            declare @TemporalTypeHere bit = 0;
            set @cmd = N'select @TemporalTypeHere = 1 from sys.all_columns where name = ''temporal_type'' and object_id = object_id(''sys.tables'')';
            exec sp_executesql @cmd, N'@TemporalTypeHere bit output', @TemporalTypeHere output;
			if @TemporalTypeHere = 1
			begin
                set @cmd = N'
	declare @versionOff nvarchar(4000) = N'''';
	select @versionOff = @versionOff + N''alter table '' + QUOTENAME( @SchemaName) + N''.'' + QUOTENAME( t.name) + N'' set( SYSTEM_VERSIONING = OFF );''
		from sys.tables t
        inner join sys.schemas s on s.schema_id = t.schema_id
    where s.name = @SchemaName and t.temporal_type = 2;
    exec sp_executesql @versionOff;';

				exec sp_executesql @cmd, N'@SchemaName sysname', @SchemaName;
			end
        end

        declare @C4 cursor;
		set @C4 = cursor local read_only for 
            select TABLE_NAME
            from INFORMATION_SCHEMA.TABLES
            where TABLE_SCHEMA = @SchemaName
                    and TABLE_NAME not like 'sys%';
        open @C4;
        fetch from @C4 into @tName;
		while @@FETCH_STATUS = 0
		begin
            set @cmd = N'drop table '+QUOTENAME( @SchemaName)+'.'+QUOTENAME( @tName);
        begin try
				exec sp_executesql @cmd;
				set @DeleteCount = @DeleteCount + 1;
        end try
			begin catch
				select
                    @ErrorMessage = ERROR_MESSAGE(),
                    @ErrorNumber = ERROR_NUMBER(),
                    @ErrorSeverity = ERROR_SEVERITY(),
                    @ErrorState = ERROR_STATE(),
                    @ErrorLine = ERROR_LINE(),
                    @ErrorProcedure = ISNULL( ERROR_PROCEDURE(), '-' );
				if @ErrorNumber = 3729
				begin
                    set @DependencyError = @DependencyError + 1;
				end
				else
				begin
                    select @ErrorMessage = N'Error %d, Level %d, State %d, Procedure %s, Line %d, ' + 'Message: ' + @ErrorMessage;

                    raiserror( @ErrorMessage, @ErrorSeverity, 1, @ErrorNumber, @ErrorSeverity, @ErrorState, @ErrorProcedure, @ErrorLine )

                end
            end catch
            fetch next from @C4 into @tName;
        end
    end
end
GO
--
-- This stored procedure changes the value of a field that is typically a key referenced by 
-- many foreign keys (recursively).
-- It works by first computing the transitive closure of the foreign keys on the column to change,
-- then it disables all the foreign key constraints it found (and only these ones), updates all the columns 
-- and reenables the constraints.
--
-- The operation is transacted. At worst an error is raised but no data should be compromised.
-- On success, 0 is returned, -1 on error.
--
-- Example:
--      @SchemaName = 'CK'      -- This is the schema of the target table.
--      @TableName = 'tActor'   -- The name of the table.
--      @ColumnName = 'ActorId' -- The name of the column.
--      @ExistingValue = '2'    -- The value to update. This must be provided as a the string representation of the value.
--      @NewValue = '3712'      -- The new value (also its string representation). 
--                                 This has typically been ""allocated"" before the call: it must exist in the target table.
--
-- The name of this procedure is intentionally stupid.
-- The violence implied by this neologism should dissuade pusillanimous users from using it.
--
create procedure CKCore.sRefBazookation
    @SchemaName sysname,
    @TableName sysname,
    @ColumnName sysname,
    @ExistingValue nvarchar(max),
    @NewValue nvarchar(max)
as
begin
    declare @DisableC nvarchar(max);
    declare @SetValue nvarchar(max);
    declare @EnableC nvarchar(max);

    with rec as ( select 
                       -- We aggregate the constraint identifier in a string to skip cycles.
                       CCId =  ';' + cast( fc.constraint_object_id as varchar(max)) + ';',
                       STId = fc.parent_object_id,
                       SCId = fc.parent_column_id,
                       TTId = fc.referenced_object_id,
                       TCId = fc.referenced_column_id,
                       CName = QUOTENAME( f.name ),
                       STable = QUOTENAME( SCHEMA_NAME( oS.schema_id ) ) + '.' + QUOTENAME( OBJECT_NAME( f.parent_object_id ) ),
                       SColumn = QUOTENAME( COL_NAME( fc.parent_object_id, fc.parent_column_id ) )
                   from sys.foreign_key_columns as fc
                   inner join sys.foreign_keys as f on f.object_id = fc.constraint_object_id
                   inner join sys.objects oS on oS.object_id = fc.parent_object_id
                   inner join sys.objects oT on oT.object_id = f.referenced_object_id
                   inner join sys.columns cT on cT.object_id = f.referenced_object_id and cT.column_id = fc.referenced_column_id
                   where oT.schema_id = SCHEMA_ID(@SchemaName) and oT.name = @TableName and cT.name = @ColumnName
                 
              union all
                
                select CCId = r.CCId + cast( fc.constraint_object_id as varchar ) + ';',
                       STId = fc.parent_object_id,
                       SCId = fc.parent_column_id,
                       TTId = fc.referenced_object_id,
                       TCId = fc.referenced_column_id,
                       CName = QUOTENAME( f.name ),
                       STable = QUOTENAME( SCHEMA_NAME( oS.schema_id ) ) + '.' + QUOTENAME( OBJECT_NAME( f.parent_object_id ) ),
                       SColumn = QUOTENAME( COL_NAME( fc.parent_object_id, fc.parent_column_id ) )
                   from rec r
                   inner join sys.foreign_key_columns as fc on fc.referenced_object_id = r.STId and fc.referenced_column_id = r.SCId 
                                                                -- This is where cycles are handled.
                                                                and r.CCId  not like '%;' + cast( fc.constraint_object_id as varchar ) + ';%' 
                   inner join sys.foreign_keys as f on f.object_id = fc.constraint_object_id
                   inner join sys.objects oS on oS.object_id = fc.parent_object_id ),
        scripts as (
            select DisableConstraint = N'alter table ' + STable + N' nocheck constraint ' + CName + N';',
                   SetValue = N'update ' + STable + N' set ' + SColumn + N' = ' + @NewValue + N' where ' + SColumn + N' = ' + @ExistingValue + N';',
                   EnableConstraint = N'alter table ' + STable + N' check constraint ' + CName + N';'
                from rec ),
        finalConstraints as (
            select D = STRING_AGG( DisableConstraint, N'' ),
                   E = STRING_AGG( EnableConstraint, N'' ) 
                from scripts ),
        finalSetter as (
            select S = STRING_AGG( s.SetValue, N'' ) from (select distinct SetValue from scripts) s )
    select @DisableC = c.D, @SetValue = s.S, @EnableC = c.E 
        from finalConstraints c 
        cross join finalSetter s;
    
    -- We use the ""Atomic"" transaction trick. 
    set nocount on; declare @SPCallTC int = @@TRANCOUNT, @SPCallId sysname; 
    beginsp:
    if @SPCallTC = 0 begin tran;
    else
    begin
        set @SPCallId = cast(32*cast(@@PROCID as bigint)+@@NESTLEVEL as varchar);
        save transaction @SPCallId;
    end
    begin try
        exec sp_executesql @DisableC;
        exec sp_executesql @SetValue;
        exec sp_executesql @EnableC;
    end try
    begin catch
        if @SPCallTC = 0 rollback;
        else if XACT_STATE() = 1 rollback transaction @SPCallId;
        exec CKCore.sErrorRethrow @@ProcId;
        return -1;
    end catch;
    endsp:
    if @SPCallTC = 0 commit;
    return 0;
end
GO
create view CKCore.vConstraintColumns
as
select distinct	TableSchema = uk.TABLE_SCHEMA, 
				TableName = uk.TABLE_NAME, 
				ConstraintName = uk.CONSTRAINT_NAME,
				Columns = Stuff((select N',' + QuoteName(COLUMN_NAME)
									from INFORMATION_SCHEMA.KEY_COLUMN_USAGE ukC
									where ukC.CONSTRAINT_SCHEMA = uk.CONSTRAINT_SCHEMA and ukC.CONSTRAINT_NAME = uk.CONSTRAINT_NAME 
									order by ORDINAL_POSITION
									for xml path(''),TYPE).value('text()[1]','varchar(max)'),1,1,N'')
			from INFORMATION_SCHEMA.KEY_COLUMN_USAGE uk
GO
create view CKCore.vForeignKeyDetail
as
	with sysfk( CONSTRAINT_SCHEMA,		
				CONSTRAINT_NAME,			
				TABLE_SCHEMA,			
				TABLE_NAME,				
				COLUMN_NAME,				
				UNIQUE_CONSTRAINT_SCHEMA,
				UNIQUE_CONSTRAINT_NAME,	
				UNIQUE_TABLE_SCHEMA,	
				UNIQUE_TABLE_NAME,		
				UNIQUE_COLUMN_NAME )
			as ( select CONSTRAINT_SCHEMA		 = source.CONSTRAINT_SCHEMA,
						CONSTRAINT_NAME			 = source.CONSTRAINT_NAME, 
						TABLE_SCHEMA			 = source.TABLE_SCHEMA,
						TABLE_NAME				 = source.TABLE_NAME, 
						COLUMN_NAME				 = source.COLUMN_NAME,
						UNIQUE_CONSTRAINT_SCHEMA = dest.CONSTRAINT_SCHEMA,
						UNIQUE_CONSTRAINT_NAME	 = dest.CONSTRAINT_NAME, 
						UNIQUE_TABLE_SCHEMA		 = dest.TABLE_SCHEMA,
						UNIQUE_TABLE_NAME		 = dest.TABLE_NAME,
						UNIQUE_COLUMN_NAME		 = dest.COLUMN_NAME
					from INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r
					inner join INFORMATION_SCHEMA.KEY_COLUMN_USAGE source 
						on source.CONSTRAINT_CATALOG = r.CONSTRAINT_CATALOG 
							and source.CONSTRAINT_SCHEMA = r.CONSTRAINT_SCHEMA 
							and source.CONSTRAINT_NAME = r.CONSTRAINT_NAME
							and source.ORDINAL_POSITION = 1
					inner join INFORMATION_SCHEMA.KEY_COLUMN_USAGE dest 
						on dest.CONSTRAINT_CATALOG = r.UNIQUE_CONSTRAINT_CATALOG 
							and dest.CONSTRAINT_SCHEMA = r.UNIQUE_CONSTRAINT_SCHEMA 
							and dest.CONSTRAINT_NAME = r.UNIQUE_CONSTRAINT_NAME
							and dest.ORDINAL_POSITION = 1
   			)		
	select
		SourceConstraintName = f.CONSTRAINT_NAME,
		CondensedSource      = QuoteName(f.TABLE_SCHEMA) + N'.' + QuoteName(f.TABLE_NAME) + N'(' + source.Columns + N')',
		TargetConstraintName = f.UNIQUE_CONSTRAINT_NAME,
		CondensedTarget      = QuoteName(f.UNIQUE_TABLE_SCHEMA) + N'.' + QuoteName(f.UNIQUE_TABLE_NAME) + N'(' + dest.Columns + N')',
		SourceTableSchema    = f.TABLE_SCHEMA,
		SourceTableName      = f.TABLE_NAME,
		SourceColumns	     = source.Columns,
		TargetTableSchema	 = f.UNIQUE_TABLE_SCHEMA,
		TargetTableName		 = f.UNIQUE_TABLE_NAME,
		TargetColumns	     = dest.Columns
	from sysfk f
		inner join CKCore.vConstraintColumns source 
				on source.TableSchema = f.TABLE_SCHEMA 
					and source.TableName = f.TABLE_NAME
					and source.ConstraintName = f.CONSTRAINT_NAME
		inner join CKCore.vConstraintColumns dest 
				on dest.TableSchema = f.UNIQUE_TABLE_SCHEMA 
					and dest.TableName = f.UNIQUE_TABLE_NAME
					and dest.ConstraintName = f.UNIQUE_CONSTRAINT_NAME
GO
if object_id('CKCore.tInvariant') is null
begin
    create table CKCore.tInvariant
    (
	    InvariantKey varchar(96) collate LATIN1_GENERAL_BIN not null,
	    Ignored bit not null,
	    -- Must be 'select @Count = ... from ...'
	    CountSelect nvarchar(2048) not null,
	    MinValidCount int not null,
	    MaxValidCount int not null,
	    LastCount int null,
	    LastError nvarchar(2048) null,
	    LastRunDateUTC datetime2 null,
	    RunStatus as case
					    when LastRunDateUTC is null then 'Never ran' 
					    when LastError is not null then 'Fatal Error'
					    when LastCount >= MinValidCount and LastCount <= MaxValidCount then 'Success'
					    else 'Failed'
				     end
	    constraint PK_CKCore_Invariant primary key( InvariantKey )
    );
end
else
begin
    declare @curVer smallint;
    select @curVer = Ver from CKCore.tSystem where Id=1;
    if @curVer < 12
    begin
        alter table CKCore.tInvariant drop column RunStatus;
        alter table CKCore.tInvariant 
            add RunStatus as case
        					    when LastRunDateUTC is null then 'Never ran' 
        					    when LastError is not null then 'Fatal Error'
        					    when LastCount >= MinValidCount and LastCount <= MaxValidCount then 'Success'
        					    else 'Failed'
        				     end
    end
end
GO
-- Registers an invariant: it is any query that returns a count and a min and max values for the count.
-- The prefix 'select @Count = count(*) ' is automatically added (if @CountSelect doesn't already start with 'select').
-- example:
--
--   exec CKCore.sInvariantRegister 'AutoSample', 'from CK.tInvariant where upper(left(CountSelect,6)) <> 'SELECT', 0, 0;
--   
-- Registers an invariant that will obviously ALWAYS be satisfied :)
--
-- To unregister, simply call CKCore.sInvariantRegister 'AutoSample', null (or '' empty string).
--
create procedure CKCore.sInvariantRegister 
(
	@InvariantKey varchar(96),
	@CountSelect nvarchar(2048),
	@MinValidCount int = 0,
	@MaxValidCount int = 0
)
as
begin
	set nocount on;
	set @CountSelect = rtrim(ltrim(@CountSelect));
	if len(@CountSelect) = 0 set @CountSelect = null;
	else if upper(left(@CountSelect,6)) <> 'SELECT' set @CountSelect = N'select @Count = count(*) ' + @CountSelect;
	merge CKCore.tInvariant as target
		using (select @InvariantKey) as source( InvariantKey )
		on target.InvariantKey = source.InvariantKey
		when matched and @CountSelect is not null then 
			update set CountSelect = @CountSelect, 
						MinValidCount = @MinValidCount,
						MaxValidCount = @MaxValidCount,
						LastCount = null,
						LastRunDateUTC = null,
						LastError = null
		when matched and @CountSelect is null then delete
		when not matched and @CountSelect is not null then
			insert( InvariantKey, Ignored, CountSelect, MinValidCount, MaxValidCount )
			values( @InvariantKey, 0, @CountSelect, @MinValidCount, @MaxValidCount );
end
GO
-- Updates CKCore.tInvariant.LastCount, LastRunDateUTC and LastError errors for the given
-- invariant. The invariant must exist otherwise an exception is thrown.
-- When setting @SkipIgnore to 1, even an invariant with CKCore.tInvariant.Ignored bit set to 1 is executed.
create procedure CKCore.sInvariantRun
( 
	@InvariantKey varchar(96), 
	@SkipIgnore bit = 0, 
	@Success bit  = null output
)
as
begin
	set nocount on;
	set @Success = null;
	declare @Count int;
	declare @sql nvarchar(2048);
	select @sql = CountSelect 
		from CKCore.tInvariant 
		where InvariantKey = @InvariantKey and (@SkipIgnore = 1 or Ignored = 0);
	if @sql is null throw 50000, 'CKCore.InvariantNotFound', 1;
	begin try
		exec sp_executesql @sql, N'@Count int output', @Count = @Count output;
		update CKCore.tInvariant 
			set LastCount = @Count, LastRunDateUTC = SYSUTCDATETIME(), LastError = null
			where InvariantKey = @InvariantKey;
		set @Success = 1;
	end try
	begin catch
		update CKCore.tInvariant 
			set LastCount = null, LastRunDateUTC = SYSUTCDATETIME(), LastError = ERROR_MESSAGE()
			where InvariantKey = @InvariantKey;
		set @Success = 0;
	end catch
	return 0;
end
GO
-- Updates CKCore.tInvariant.LastCount, LastRunDateUTC and LastError errors for  all invariants.
-- When setting @SkipIgnore to 1, even an invariant with CKCore.tInvariant.Ignored bit set to 1 is executed.
create procedure CKCore.sInvariantRunAll
( 
	@SkipIgnore bit = 0, 
	@Success bit = null output 
)
as
begin
	set nocount on;
	set @Success = null;
	declare @InvariantKey varchar(96);
	declare @CInv cursor;
	set @CInv = cursor local fast_forward for 
		select InvariantKey from CKCore.tInvariant where @SkipIgnore = 1 or Ignored = 0;
	open @CInv;
	fetch from @CInv into @InvariantKey;
	while @@FETCH_STATUS = 0
	begin
		declare @oneSuccess bit;
		exec CKCore.sInvariantRun @InvariantKey, @SkipIgnore, @oneSuccess output;
		if @oneSuccess = 1 and @Success is null set @Success = 1;
		if @oneSuccess = 0 set @Success = 0;
		fetch next from @CInv into @InvariantKey;
	end
	deallocate @CInv;
end
GO
if object_id('CKCore.tSystem') is null
begin
	create table CKCore.tSystem
	(
		Id int not null,
        CreationDate SmallDateTime not null,
        Ver smallint not null,
        constraint PK_tSystem primary key (Id),
		constraint CK_tSystem_Id check (Id in (1,2))
	);
end
declare @curVer smallint;
select @curVer = Ver from CKCore.tSystem where Id=1;
if @@RowCount = 0 
begin
    insert into CKCore.tSystem(Id,CreationDate,Ver) values(1,GETUTCDATE(),$Ver$);
end
else
begin
    update CKCore.tSystem set Ver = $Ver$ where Id=1;
end
";
    }
}
