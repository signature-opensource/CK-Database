--[beginscript]

create table CK.tActorProfile
(
	ActorId int not null,
	GroupId int not null,
	constraint PK_tActorProfile primary key nonclustered (ActorId,GroupId),
	constraint FK_tActorProfile_ActorId foreign key(ActorId) references CK.tActor( ActorId ),
	constraint FK_tActorProfile_GroupId foreign key(GroupId) references CK.tActor( ActorId )
); 

insert into CK.tActorProfile( ActorId, GroupId ) values( 0, 0 );
insert into CK.tActorProfile( ActorId, GroupId ) values( 1, 1 );

--[endscript]
