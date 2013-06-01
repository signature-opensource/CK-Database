
create table CK.tRes 
(
	ResId int not null identity (0, 1),
	ResName varchar(96) collate Latin1_General_BIN not null,
	constraint PK_tRes primary key nonclustered (ResId)
);

create unique clustered index IX_tRes ON CK.tRes(ResName);

insert into CK.tRes( ResName ) values( '' );
insert into CK.tRes( ResName ) values( 'System' );
