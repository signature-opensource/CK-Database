
create table CK.tResDataString
(
	ResId int not null,
	Val nvarchar (400) collate database_default not null,
	RowVer rowversion not null,
	constraint PK_tResStringData primary key clustered (ResId),
	constraint FK_tResStringData_tRes foreign key (ResId) references CK.tRes( ResId )
);

insert into CK.tResDataString( ResId, Val) values( 0, N'');
insert into CK.tResDataString( ResId, Val ) values( 1, N'Ce Système...' )
