-- Version = *
create procedure CK.sSimpleTransformTest
(
    @TextParam nvarchar(20) output
)
as
begin
	set @TextParam = N'No!';
	return 0;
end
