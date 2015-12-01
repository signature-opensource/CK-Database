-- Version = 2.12.2, Requires = { CK.fUserIsInGroup }
create view CK.vUser
as	
	select 
		u.UserId,
		u.UserName,
		u.Email
	from CK.tUser u
	inner join CK.tActor a on a.ActorId = u.UserId;
	