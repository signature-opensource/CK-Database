
create table CK.tResDataRawText
(
	ResId int not null,
	Val nvarchar(max) collate database_default not null,
	RowVer rowversion not null,
	constraint PK_tResDataRawText primary key clustered( ResId ),
	constraint FK_tResDataRawText_tRes foreign key( ResId ) references CK.tRes( ResId ),
);

insert into CK.tResDataRawText( ResId, Val) values( 0, N'');

