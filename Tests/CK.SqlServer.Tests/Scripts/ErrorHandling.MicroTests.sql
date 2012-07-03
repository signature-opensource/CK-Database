--
-- Calling sTestError
--
go -- no bug, no transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestError 'none';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- no bug, transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestError 'none';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- soft bug, no transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestError 'bug', 'soft';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- soft bug, retry, no transaction. 
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestError 'retry', 'soft';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- soft bug, transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestError 'bug', 'soft';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- soft bug, retry, transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestError 'retry', 'soft';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- hard bug, no transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestError 'bug', 'hard';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- hard bug, transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestError 'bug', 'hard';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- hard bug, retry, no transaction.
   -- There is no outer transaction: the "doomed" transaction has been rolled back and the retry IS possible.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestError 'retry', 'hard';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- hard bug, retry, transaction. EXCEPTION.
   -- We are in a "doomed" transaction: retry is not possible.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestError 'retry', 'hard';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- Code Throws bug, no transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestError 'bug', 'Code Throws';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- Code Throws bug, transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestError 'bug', 'Code Throws';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- Code Throws bug, retry, no transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestError 'retry', 'Code Throws';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- Code Throws bug, retry, transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestError 'retry', 'Code Throws';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

--
-- Calling sTestErrorCall that calls sTestError.
--

go -- no bug, no transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'none', '';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- no bug, transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestErrorCall 'none', 'none', '';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- soft bug, no transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'bug', 'soft';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- soft bug, retry, no transaction. 
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'retry', 'soft';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- soft bug, transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestErrorCall 'none', 'bug', 'soft';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- soft bug, retry, transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestErrorCall 'none', 'retry', 'soft';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- hard bug, no transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'bug', 'hard';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- hard bug, transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestErrorCall 'none', 'bug', 'hard';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- hard bug, retry, no transaction. EXCEPTION.
   -- The sTestErrorCall inner transaction is in a "doomed" state: retry from inside sTestError is not possible.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'retry', 'hard';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- hard bug, retry, no transaction with a retry at the sTestErrorCall level.
   -- Since there is no outer transaction, sTestErrorCall rollbacks its "doomed" transaction and can retry sTestError (with 'none').
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'retryCallWithNone', 'retry', 'hard';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- hard bug, retry, transaction with a retry at the sTestErrorCall level. EXCEPTION.
   -- Same as above, except that an outer transaction prevents sTestErrorCall to rollbacks its own "doomed" transaction: retry is not possible.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran;
exec @ret = CKCoreTests.sTestErrorCall 'retryCallWithNone', 'retry', 'hard';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- hard bug, retry, transaction. EXCEPTION.
   -- We are in a "doomed" transaction: retry is not possible.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'retry', 'hard';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- A syntax error in dynamic sql exec('syntax-error'). EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'bug', 'none', '';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- A syntax error in dynamic sql exec('syntax-error') is recoverable.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'retry', 'none', '';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- hard bug, retry, transaction - after a retry with the exec('syntax error'). EXCEPTION.
   -- We are in a "doomed" transaction: retry is not possible.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestErrorCall 'retry', 'retry', 'hard';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- Code Throws bug, no transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'bug', 'Code Throws';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- Code Throws bug, transaction. EXCEPTION.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestErrorCall 'none', 'bug', 'Code Throws';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, -1, 0;

go -- Code Throws bug, retry, no transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
exec @ret = CKCoreTests.sTestErrorCall 'none', 'retry', 'Code Throws';
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;

go -- Code Throws bug, retry, transaction.
set nocount on; exec CKCoreTests.sTestNonTransactedIdReset; declare @ret int;
begin tran
exec @ret = CKCoreTests.sTestErrorCall 'none', 'retry', 'Code Throws';
if @ret = 0 commit; else rollback;
exec CKCoreTests.sTestErrorMicroTestResultSet @ret, 0, 2;
