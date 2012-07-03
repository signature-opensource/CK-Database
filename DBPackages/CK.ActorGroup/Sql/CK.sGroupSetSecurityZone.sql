-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Update the security zone id of the given group.
--
create procedure CK.sGroupSetSecurityZone
(
	@ActorId int,
	@GroupId int,
	@SecurityZoneId int
)
as begin
	
	if exists (select * from CK.tGroup where GroupId = @GroupId)
	begin
		update CK.tGroup set SecurityZoneId = @SecurityZoneId 
			where GroupId = @GroupId;
		return 0;
	end
	
	return 1;
end