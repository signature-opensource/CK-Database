
create table CK.tSecurityZone 
(
	SecurityZoneId int not null,
	ZoneName varchar(12) not null,
	
	constraint PK_tSecurityZone primary key clustered ( SecurityZoneId ),
	constraint FK_tSecurityZone_tGroup foreign key( SecurityZoneId ) references CK.tGroup( GroupId ),
	constraint UK_tSecurityZone_ZoneName unique ( ZoneName )
);
insert into CK.tSecurityZone( SecurityZoneId, ZoneName ) values ( 0, 'Public' );
insert into CK.tSecurityZone( SecurityZoneId, ZoneName ) values ( 1, 'System' );
