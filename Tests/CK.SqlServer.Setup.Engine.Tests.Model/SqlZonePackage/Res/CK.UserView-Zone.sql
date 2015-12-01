-- Version = 2.12.2
create view CK.vUser
as	
	select 
		u.UserId,
		u.UserName,
		u.Email,
		z.SecurityZoneId,
		z.ZoneName
	from CK.tUser u
	inner join CK.tActor a on a.ActorId = u.UserId
	inner join CK.tActorProfile ap on ap.ActorId = a.ActorId
	inner join CK.tSecurityZone z on z.SecurityZoneId = ap.GroupId;
	