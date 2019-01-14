-- SetupConfig: {}
--    This does not work: transaction level is restored at the end of a sp call. 
create procedure CK.sTransactSetLevelNotWorking
(
    @IsolationLevelBeforeResult varchar(128) output
)
as
begin

    select @IsolationLevelBeforeResult =
            case
                when transaction_isolation_level = 1 then 'READ UNCOMMITTED'
                when transaction_isolation_level = 2 and is_read_committed_snapshot_on = 1 THEN 'READ COMMITTED SNAPSHOT'
                when transaction_isolation_level = 2 and is_read_committed_snapshot_on = 0 THEN 'READ COMMITTED'
                when transaction_isolation_level = 3 then 'REPEATABLE READ'
                when transaction_isolation_level = 4 then 'SERIALIZABLE'
                when transaction_isolation_level = 5 then 'SNAPSHOT'
                else null
            end
        from sys.dm_exec_sessions s
        cross join sys.databases d
        where session_id = @@SPID and d.database_id = DB_ID();

    set transaction isolation level SERIALIZABLE;
    
    return 0;
end

