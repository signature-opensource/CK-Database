using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    internal class SqlCKCoreInstaller
    {
        public readonly static Int16 CurrentVersion = 2;

        /// <summary>
        /// Installs the kernel.
        /// </summary>
        /// <param name="manager">The manager that will be used.</param>
        /// <param name="logger">The logger to use.</param>
        /// <returns>True on success.</returns>
        public static bool Install( SqlManager manager, IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );

            using( logger.OpenGroup( LogLevel.Trace, "Installing CKCore kernel." ) )
            {
                using( logger.Filter( LogLevelFilter.Error ) )
                {
                    SimpleScriptTagHandler s = new SimpleScriptTagHandler( _script.Replace( "$Ver$", CurrentVersion.ToString() ) );
                    if( !s.Expand( logger, false ) ) return false;
                    if( !manager.ExecuteScripts( s.SplitScript().Select( one => one.Body ), logger ) ) return false;
                }
                logger.CloseGroup( String.Format( "Installed in version {0}.", CurrentVersion ) );
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
if not exists( select * from CKCore.tSystem where Id=1 )
begin
    insert into CKCore.tSystem(Id,CreationDate,Ver) values(1,GETUTCDATE(),$Ver$);
end
";
        /* CKCore should NOT create CK schema.
    GO
    if not exists(select 1 from sys.schemas where name = 'CK')
    begin
        exec( 'create schema CK' );
    end

         */
    }
}
