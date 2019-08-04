-- SetupConfig: {}
create procedure CK.sSleepProc( @SleepTime int )
as
begin
    declare @DelayLength char(8) = '00:00:' + replace(str(@SleepTime, 2, 0), ' ', '0');
    WaitFor delay @DelayLength;
end
