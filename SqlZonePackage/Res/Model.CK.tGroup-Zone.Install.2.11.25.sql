
alter table CK.tGroup add SecurityZoneId int not null constraint DF_TEMP default(0);
alter table CK.tGroup drop DF_TEMP;

update CK.tGroup set SecurityZoneId = 1 where GroupId = 1;

alter table CK.tGroup drop UK_tGroup_GroupName;

alter table CK.tGroup add 
	constraint FK_Group_SecurityZoneId foreign key references CK.tSecurityZone( SecurityZoneId ),
	constraint UK_tGroup_SecurityZoneId_GroupName unique ( SecurityZoneId, GroupName );

