-- Version = *
create procedure CK.sSimpleTransformTest
(
    @TextParam output
)
as
begin
	set @TextParam = N'No!';
	return 0;
end
