if exists(select 1 from sys.schemas where name = 'CKCoreTests')
begin
	-- Cleanup if needed (recreated by the reset procedure).
	if object_id('CKCoreTests.tTestLog') is not null drop table CKCoreTests.tTestLog;
	if object_id('CKCoreTests.tTestNonTransactedId') is not null drop table CKCoreTests.tTestNonTransactedId;
end
else
begin
	exec( 'create schema CKCoreTests' );
end
go
--
-- Tests support.
--
if object_id('CKCoreTests.tTestErrorLogTestResult') is not null drop table CKCoreTests.tTestErrorLogTestResult;
create table CKCoreTests.tTestErrorLogTestResult( Error varchar(128) );
go
if object_id('CKCoreTests.sTestNonTransactedIdReset') is not null drop procedure CKCoreTests.sTestNonTransactedIdReset;
go
create procedure CKCoreTests.sTestNonTransactedIdReset
as
begin
	if object_id('CKCoreTests.tTestLog') is not null drop table CKCoreTests.tTestLog;
	if object_id('CKCoreTests.tTestNonTransactedId') is not null drop table CKCoreTests.tTestNonTransactedId;
	create table CKCoreTests.tTestNonTransactedId
	(
		Id int not null identity(1,1),
		constraint PK_TestNonTransactedId primary key ( Id )
	);

	create table CKCoreTests.tTestLog
	(
		Id int not null,
		Leave bit not null,
		Msg nvarchar(256) not null,
		constraint FK_TestNonTransactedId foreign key( Id ) references CKCoreTests.tTestNonTransactedId( Id )
	);
end
go
if object_id('CKCoreTests.sTestLog') is not null drop procedure CKCoreTests.sTestLog;
go
create procedure CKCoreTests.sTestLog
	@Leave bit,
	@Msg nvarchar(256)
as
begin
	insert into CKCoreTests.tTestNonTransactedId default values;
	insert into CKCoreTests.tTestLog( Id, Leave, Msg ) select SCOPE_IDENTITY(), @Leave, @Msg;
end
go
if object_id('CKCoreTests.sTestError') is not null drop procedure CKCoreTests.sTestError;
go
create procedure CKCoreTests.sTestError
(
	@DoBug varchar(100) = '',		-- 'none', 'bug', 'retry'.
	@BugType varchar(100) = 'soft'	-- 'soft', 'hard', other (raiserror).
)
as
begin

--[=beginsp]
	set nocount on;
	declare @SPCallTC int = @@TRANCOUNT, @SPCallId sysname;
	beginsp:
	if @SPCallTC = 0 begin tran; 
	else 
	begin 
		set @SPCallId = cast(32*cast(@@PROCID as bigint)+@@NESTLEVEL as varchar);
		save transaction @SPCallId;
	end
	begin try
--[=/beginsp]

		exec CKCoreTests.sTestLog 0, 'sTestError';
		if @DoBug <> 'none'
		begin
			if @BugType = 'hard' 
			begin
				-- This conversion attempt triggers a 'Batch Aborting' error (and, please, don't ask why...).
				declare @BatchAborts datetime = convert(datetime, '2008111');
			end
			else if @BugType = 'soft' 
			begin
				-- This concersion attempts triggers a non 'Batch Aborting' error.
				declare @BatchContinues datetime = convert(datetime, '20081311');
				-- Just as a constraint violation.
				-- insert into CKCore.tSystem default values;
			end
			else
			begin
				-- This is the way to throw an error in Sql Server 2005. Version 2008 comes with a 'throw'.
				raiserror( N'Raised Error from code: BugType = %s', 16, 1, @BugType );
			end
		end
		exec CKCoreTests.sTestLog 1, 'sTestError';
--[=endsp]
	end try
	begin catch
		--
		-- This code was one of the possibility to offer --[onError](?) handling (It was actually my first idea.)
		-- It is deprecated in favor of the code beeing AFTER the rollback (see below).
		-- (This changed the beginsp: label position.)
		--
		--	-- This is to show a 'retry' possibility.
		--	-- This is not possible in doomed transaction: any logged operation fails.
		--	if XACT_STATE() = 1
		--	begin
		--		-- This is the --[onErrorInTran] (or --[onErrorRetry], --[onErrorCanRetry]?) 
		--		-- Here comes the retry logic.
		--		if @DoBug = 'retry' 
		--		begin
		--			set @DoBug = 'none';
		--			-- We should correct any side effect since we manually handle 
		--			-- the error and do not rely on the transaction.
		--			-- Here we cleanup the last log entry.
		--			delete from CKCoreTests.tTestLog where Id in (select top 1 Id from CKCoreTests.tTestLog order by Id desc);
		--			goto beginsp;
		--		end
		--		-- End of --[onErrorInTran]
		--	end
		if @SPCallTC = 0 rollback;
		else if XACT_STATE() = 1 rollback transaction @SPCallId;
		
		--
		-- As of may 18, 2012, the best name seems to be: --[onRecoverableError] 
		--
		-- This is the 'OnRecoverableError', we are outside the inner transaction: it seems, after thougths, that
		-- this is much more simpler to understand for the developper since beeing "out of" the inner transaction scope
		-- the code here is does not have to deal with reverting what has been done in the body (the rollback just did it).
		-- Note: This is not possible in doomed transaction since any logged operation fails.
		if XACT_STATE() <> -1
		begin
--[=onRecoverableError]
			if @DoBug = 'retry' 
			begin
				set @DoBug = 'none';
				-- We do not have any cleanup to do here.
				goto beginsp;
			end
--[=/onRecoverableError]
		end
		exec CKCore.sErrorRethrow @@ProcId;
		return -1;
	end catch;
	endsp:
	if @SPCallTC = 0 commit;
	return 0;
--[=/endsp]

end
go
if object_id('CKCoreTests.sTestErrorCall') is not null drop procedure CKCoreTests.sTestErrorCall;
go
create procedure CKCoreTests.sTestErrorCall
(
	@DoBug varchar(100),
	@SubDoBug varchar(100),
	@SubBugType varchar(100)
)
as
begin
--[=beginsp]
	set nocount on;
	declare @SPCallTC int = @@TRANCOUNT, @SPCallId sysname;
	beginsp:
	if @SPCallTC = 0 begin tran; 
	else 
	begin 
		set @SPCallId = cast(32*cast(@@PROCID as bigint)+@@NESTLEVEL as varchar);
		save transaction @SPCallId;
	end
	begin try
--[=/beginsp]

		if @DoBug <> 'none' and @DoBug <> 'retryCallWithNone'
		begin
			exec( 'syntax-error.' );
		end
		exec CKCoreTests.sTestError @SubDoBug, @SubBugType;

--[=endsp]
	end try
	begin catch
		if @SPCallTC = 0 rollback;
		else if XACT_STATE() = 1 rollback transaction @SPCallId;
		if XACT_STATE() <> -1
		begin
--[=onRecoverableError]
			-- We do not have any cleanup to do here.
			if @DoBug = 'retry' 
			begin
				set @DoBug = 'none';
				goto beginsp;
			end
			if @DoBug = 'retryCallWithNone'
			begin
				set @SubDoBug = 'none';
				goto beginsp;
			end
--[=/onRecoverableError]
		end
		exec CKCore.sErrorRethrow @@ProcId;
		return -1;
	end catch;
	endsp:
	if @SPCallTC = 0 commit;
	return 0;
--[=/endsp]
end
go
if object_id('CKCoreTests.sTestErrorMicroTestResultSet') is not null drop procedure CKCoreTests.sTestErrorMicroTestResultSet;
go
create procedure CKCoreTests.sTestErrorMicroTestResultSet
( 
	@ActualReturnValue int,
	@ExpectedReturnValue int,
	@ExpectedNbActions int
)
as
begin
	-- Display the result when executing in Console.
	select 'Result', 'Return Value' = @ActualReturnValue, 'TranCount' = @@TRANCOUNT, 'NbActions' = count(*) from CKCoreTests.tTestLog 
	union select 'Expected', @ExpectedReturnValue, 0, @ExpectedNbActions;

	declare @Error varchar(128) = '';
	if @ActualReturnValue <> @ExpectedReturnValue set @Error = @Error + '[ReturnValue]';
	if @@TranCount <> 0 set @Error = @Error + '[TranCount]';
	if (select count(*) from CKCoreTests.tTestLog) <> @ExpectedNbActions set @Error = @Error + '[NbActions]';

	truncate table CKCoreTests.tTestErrorLogTestResult;
	insert into CKCoreTests.tTestErrorLogTestResult( Error ) values( @Error );
end
go

