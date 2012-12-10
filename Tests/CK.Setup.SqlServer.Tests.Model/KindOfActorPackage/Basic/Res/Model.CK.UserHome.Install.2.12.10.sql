--[beginscript]

create table CK.tUser 
(
	UserId int not null,
	UserName nvarchar( 255 ) collate LATIN1_GENERAL_BIN not null,
	Email varchar( 255 ) collate LATIN1_GENERAL_BIN not null,
	CreationDate datetime not null constraint DF_tUser_CreationDate default( getutcdate() ),
	IsEnabled bit not null constraint DF_tUser_IsEnabled default( 1 ),

	constraint PK_tUser primary key clustered( UserId ),
	constraint FK_tUser_tActor foreign key ( UserId ) references CK.tActor( ActorId ),
	constraint UK_tUser_UserName unique ( UserName )
);
--
insert into CK.tUser( UserId, UserName, Email ) values( 0, '', '' );
insert into CK.tUser( UserId, UserName, Email ) values( 1, 'System', '' );

--[endscript]
