-- Version = 2.12.2
create function CK.fUserIsInGroup
(
	@UserId int,
	@GroupId int
)
returns bit 
as 
begin
	return case when exists( select * from CK.tActorProfile where ActorId = @UserId and GroupId = @GroupId ) then 1 else 0 end;
end

