using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    internal class SqlCKCoreInstaller
    {
        public readonly static short CurrentVersion = 12;

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

            using( monitor.OpenTrace().Send( "Installing CKCore kernel." ) )
            {
                short ver = 0;
                if( !forceInstall && (ver = (short)manager.Connection.ExecuteScalar( "if object_id('CKCore.tSystem') is not null select Ver from CKCore.tSystem where Id=1 else select cast(0 as smallint);" )) == CurrentVersion )
                {
                    monitor.CloseGroup( $"Already installed in version {CurrentVersion}." );
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
    if object_id('CKCore.sInvariantRegister') is not null drop procedure CKCore.sInvariantRegister;
    if object_id('CKCore.sInvariantRun') is not null drop procedure CKCore.sInvariantRun;
    if object_id('CKCore.sInvariantRunAll') is not null drop procedure CKCore.sInvariantRunAll;
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
		set @cmd = N'alter table ['+@SchemaName+'].['+@tName+N'] drop constraint '+@cName;
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
	declare @cmd nvarchar(800);
	if @ObjectType is null or @ObjectType = 'CONSTRAINT'
	begin
		exec CKCore.sSchemaDropAllConstraints @SchemaName;
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
		    set @cmd = N'drop '+@rType+' ['+@SchemaName+'].['+@rName+N']';
		    exec sp_executesql @cmd;
		    fetch next from @C2 into @rName, @rType;
		end 
	end
	if  @ObjectType is null or @ObjectType = 'VIEW'
	begin
		declare @C cursor;
		set @C = cursor local read_only for 
			select TABLE_NAME 
			from INFORMATION_SCHEMA.VIEWS 
			where TABLE_SCHEMA = @SchemaName 
                    and TABLE_NAME not like 'sys%';
		declare @vName sysname;
		open @C;
		fetch from @C into @vName;
		while @@FETCH_STATUS = 0
		begin
			set @cmd = N'drop view ['+@SchemaName+'].['+@vName+N']';
			exec sp_executesql @cmd;
			fetch next from @C into @vName;
		end
	end
    if @ObjectType is null or @ObjectType = 'TABLE'
	begin
		declare @C3 cursor;
		set @C3 = cursor local read_only for 
			select TABLE_NAME 
			from INFORMATION_SCHEMA.TABLES 
			where TABLE_SCHEMA = @SchemaName 
                    and TABLE_NAME not like 'sys%';
		declare @tName sysname;
		open @C3;
		fetch from @C3 into @tName;
		while @@FETCH_STATUS = 0
		begin
			set @cmd = N'drop table ['+@SchemaName+'].['+@tName+N']';
			exec sp_executesql @cmd;
			fetch next from @C3 into @tName;
		end
	end
end
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
