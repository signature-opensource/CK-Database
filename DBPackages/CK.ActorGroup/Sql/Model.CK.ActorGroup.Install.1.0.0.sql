create table CK.tSecurityZone 
(
	SecurityZoneId int not null,
	ZoneName varchar(12) not null,
	
	constraint PK_tSecurityZone primary key clustered ( SecurityZoneId ) on [PRIMARY]
);

create table CK.tGroup
(
	GroupId int not null,
	SecurityZoneId int not null,
	GroupName varchar(32) not null,

	constraint PK_tGroup primary key clustered ( GroupId ) on [PRIMARY],
	constraint FK_tGroup_tActor foreign key( GroupId ) references CK.tActor(ActorId),
	constraint FK_tGroup_tSecurityZone foreign key( SecurityZoneId ) references CK.tSecurityZone(SecurityZoneId),
	constraint UK_tGroup_GroupNameSecurityZoneId unique(SecurityZoneId, GroupName)
);

create table CK.tActorProfile
(
	ActorId int not null,
	GroupId int not null,

	constraint PK_tActorProfile primary key clustered( ActorId asc, GroupId asc ) on [PRIMARY],
	constraint FK_tActorProfile_tActor foreign key( ActorId ) references CK.tActor( ActorId ),
	constraint FK_tActorProfile_tGroup foreign key( GroupId ) references CK.tGroup( GroupId )
);

-- 0 == Anonymous zone
insert into CK.tSecurityZone ( SecurityZoneId, ZoneName ) values ( 0, 'Anonymous' );
insert into CK.tGroup ( GroupId, SecurityZoneId, GroupName ) values ( 0, 0, 'Anonymous' );
insert into CK.tActorProfile ( GroupId, ActorId ) values ( 0, 0 );
-- 1 == System zone
insert into CK.tSecurityZone ( SecurityZoneId, ZoneName ) values ( 1, 'System' );
insert into CK.tGroup ( GroupId, SecurityZoneId, GroupName ) values ( 1, 1, 'System' );
insert into CK.tActorProfile ( GroupId, ActorId ) values ( 1, 1 );

alter table CK.tSecurityZone add constraint FK_tSecurityZone_tGroup foreign key(SecurityZoneId) references CK.tGroup(GroupId);