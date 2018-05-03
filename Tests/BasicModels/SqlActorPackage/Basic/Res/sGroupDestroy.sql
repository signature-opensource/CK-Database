-- SetupConfig : {}
create procedure CK.sGroupDestroy
(
	@GroupId int
)
as
begin
	--[beginsp]
	delete from CK.tGroup where GroupId = @GroupId;
	--[endsp]
end

