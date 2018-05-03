-- SetupConfig: { "Requires":[ "CK.fUserIsInGroup" ] }
create view CK.a_stupid_view --with schemabinding
as  
	select p.GroupId, p.ActorId, IsInGroup = CK.fUserIsInGroup( p.ActorId, p.GroupId )
		from CK.tActorProfile p;
