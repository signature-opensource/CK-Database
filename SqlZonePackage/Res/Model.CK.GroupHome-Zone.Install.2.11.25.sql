--[beginscript]

alter table CK.tGroup add SecurityZoneId int not null constraint DF_tGroup_SecurityZoneId default(0);

--[endscript]

--[beginscript]

update CK.tGroup set SecurityZoneId = 1 where GroupId = 1;

alter table CK.tGroup drop UK_tGroup_GroupName;

alter table CK.tGroup add 
	constraint FK_Group_SecurityZoneId foreign key (SecurityZoneId) references CK.tSecurityZone( SecurityZoneId ),
	constraint UK_tGroup_SecurityZoneId_GroupName unique ( SecurityZoneId, GroupName );

--[endscript]

