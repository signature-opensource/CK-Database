-- Version = 1.0.0
-- CKCore.tSystem.DefaultLCID is French (12)
alter table CKCore.tSystem add 
	DefaultLCID int constraint DF_tSystem_DefaultLCID default(12);

create table CK.tRes 
(
	ResId int not null identity (0, 1),
	ResName varchar(96) collate Latin1_General_BIN not null,
	constraint PK_tRes primary key nonclustered (ResId)
);

create unique clustered index IX_tRes ON CK.tRes(ResName);

create table CK.tResStringData 
(
	ResId int not null,
	Val nvarchar (400) collate database_default not null,
	RowVer rowversion not null,
	constraint PK_tResStringData primary key clustered (ResId),
	constraint FK_tResStringData_tRes foreign key (ResId) references CK.tRes (ResId)
);

create table CK.tResRawTextData
(
	ResId int not null,
	Val nvarchar(max) collate database_default not null,
	RowVer rowversion not null,
	constraint PK_tResBigTextData primary key clustered( ResId ),
	constraint FK_tResBigTextData_tRes foreign key( ResId ) references CK.tRes( ResId ),
);

GO

-- 0 == Empty string => ResName is an Empty String and texts are Empty Strings.
insert into CK.tRes( ResName ) values( '' );
insert into CK.tResStringData( ResID, Val) values( 0, N'');
insert into CK.tResRawTextData( ResID, Val ) values( 0, N'');

-- 1 == System itself.
insert into CK.tRes( ResName ) values( 'System' )
insert into CK.tResStringData( ResId, Val ) values( 1, N'Ce système...' )
