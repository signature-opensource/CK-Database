using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    internal class SqlCKCoreInstaller
    {
        public readonly static Int16 CurrentVersion = 3;

        /// <summary>
        /// Installs the kernel.
        /// </summary>
        /// <param name="manager">The manager that will be used.</param>
        /// <param name="logger">The logger to use.</param>
        /// <param name="forceInstall">True to force the installation even if Ver column of CKCore.tSystem where Id = 1 is the same as <see cref="CurrentVersion"/>.</param>
        /// <returns>True on success.</returns>
        public static bool Install( SqlManager manager, IActivityLogger logger, bool forceInstall = false )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );

            using( logger.OpenGroup( LogLevel.Trace, "Installing CKCore kernel." ) )
            {
                Int16 ver = 0;
                if( !forceInstall && (ver = (Int16)manager.Connection.ExecuteScalar( "if object_id('CKCore.tSystem') is not null select Ver from CKCore.tSystem where Id=1 else select cast(0 as smallint);" )) == CurrentVersion )
                {
                    logger.CloseGroup( String.Format( "Already installed in version {0}.", CurrentVersion ) );
                }
                else
                {
                    using( logger.Filter( LogLevelFilter.Error ) )
                    {
                        SimpleScriptTagHandler s = new SimpleScriptTagHandler( _script.Replace( "$Ver$", CurrentVersion.ToString() ) );
                        if( !s.Expand( logger, false ) ) return false;
                        if( !manager.ExecuteScripts( s.SplitScript().Select( one => one.Body ), logger ) ) return false;
                    }
                    if( ver == 0 ) logger.CloseGroup( String.Format( "Installed in version {0}.", CurrentVersion ) );
                    else logger.CloseGroup( String.Format( "Installed in version {0} (was {1}).", CurrentVersion, ver ) );
                }
            }
            return true;
        }

        static string _script = @"
if not exists(select 1 from sys.schemas where name = 'CKCore')
begin
    exec( 'create schema CKCore' );
end
else
begin
    if object_id('CKCore.sErrorRethrow') is not null drop procedure CKCore.sErrorRethrow;
    if object_id('CKCore.sSchemaDropAllConstraints') is not null drop procedure CKCore.sSchemaDropAllConstraints;
    if object_id('CKCore.sSchemaDropAllObjects') is not null drop procedure CKCore.sSchemaDropAllObjects;
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
		if not exists( select * from ::fn_listextendedproperty('CKLock', 'user', @SchemaName, 'TABLE', @tName, NULL, NULL) )
		begin
			declare @cmd nvarchar(800);
			set @cmd = N'alter table ['+@SchemaName+'].['+@tName+N'] drop constraint '+@cName;
			exec sp_executesql @cmd;
		end 
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
					and ROUTINE_NAME <> 'sSchemaDropAllObjects'
                    and not exists( select * from ::fn_listextendedproperty('CKLock', 'user', @SchemaName, ROUTINE_TYPE, ROUTINE_NAME, NULL, NULL) );
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
                    and TABLE_NAME not like 'sys%'
                    and not exists( select * from ::fn_listextendedproperty('CKLock', 'user', @SchemaName, 'VIEW', TABLE_NAME, NULL, NULL) );
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
		exec CKCore.sSchemaDropAllConstraints @SchemaName;
		set @cmd = 'declare @n sysname set @n = PARSENAME( ''?'', 1 ) if PARSENAME( ''?'', 2 ) = '''+@SchemaName+''' and not exists( select * from ::fn_listextendedproperty(''CKLock'', ''user'', '''+@SchemaName+''', ''TABLE'', @n, NULL, NULL) ) drop table ?';
		exec sp_MSforeachtable @command1 = @cmd;
	end
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
if not exists(select * from CKCore.tSystem where Id=1) insert into CKCore.tSystem(Id,CreationDate,Ver) values(1,GETUTCDATE(),$Ver$);
else update CKCore.tSystem set Ver = $Ver$ where Id=1;
";
    }
}
