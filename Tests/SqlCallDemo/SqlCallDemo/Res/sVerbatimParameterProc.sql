-- SetupConfig: {}
create procedure CK.sVerbatimParameterProc
(
    @This int,
    @Operator int,
    @Result int output
)
as
begin
    set @Result = @This + @Operator;
    return 0;
end

