-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Creates a SecurityZone.
--
create procedure CK.sSecurityZoneCreate 
(
	@ActorId int,
	@ZoneName nvarchar(12),
	@SecurityZoneIdResult int output
)
as begin

	declare @GroupId int;
	exec CK.sGroupCreate @ActorId, 0, @ZoneName, @GroupId output;
	
	insert into CK.tSecurityZone(SecurityZoneId, ZoneName) 
		values(@GroupId, @ZoneName);
	set @SecurityZoneIdResult = @GroupId;

	exec CK.sGroupSetSecurityZone @ActorId, @GroupId, @GroupId;
	
	return 0;
end