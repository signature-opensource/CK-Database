-- SetupConfig: {}
create procedure CK.sCharProc
(
    @C1 char /*not null*/,
    @C2 char,
    @CN1 nchar /*not null*/,
    @CN2 nchar,
    @CO char /*not null*/ output,
    @CNO nchar output
)
as
begin
    if @C2 is null set @CO = @C1; else set @CO = @C2;
    if @CN2 is null set @CNO = @CN1; else set @CNO = @CN2;
    return 0;
end

