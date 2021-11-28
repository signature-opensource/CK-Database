-- SetupConfig: {}
create procedure sOutputTypeCastWithDefault
(
    @ParamInt int = null,
    @ParamSmallInt smallint = null,
    @ParamTinyInt tinyint = null,
    @Result varchar(512) output
)
as
begin
    set @Result = 'ParamInt: ' + cast(@ParamInt as varchar)
                + ', ParamSmallInt: ' + cast(@ParamSmallInt as varchar)
                + ', ParamTinyInt: ' + cast( @ParamTinyInt as varchar ) + '.';
    return 0;
end

