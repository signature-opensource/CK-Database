
create table CK.tActor (
  ActorId int not null identity ( 2, 1 ),
  constraint PK_tActor primary key clustered ( ActorId )
);


create table CK.tUser (
  UserId int not null,
  UserName nvarchar( 32 ) not null,
  Email varchar( 100 ) not null,
  CreationDate DateTime not null constraint DF_tUser_CreationDate default ( getutcdate() ),
  LastModificationDate DateTime not null constraint DF_tUser_LastModificationDate default ( getutcdate() ),
  IsEnabled bit not null constraint DF_tUser_IsEnabled default ( 1 ),
  constraint PK_tUser primary key ( UserId ),
  constraint UK_tUser_UserName unique ( UserName ),
  constraint FK_tUser_tActor foreign key ( UserId ) references CK.tActor ( ActorId )
);

GO

SET IDENTITY_INSERT CK.tActor ON

GO

-- 0 == Anonymous user
insert into CK.tActor ( ActorId ) values ( 0 );
insert into CK.tUser ( UserId, UserName, Email ) values ( 0, N'Anonyme', '' );

-- 1 == System user
insert into CK.tActor ( ActorId ) values ( 1 );
insert into CK.tUser ( UserId, UserName, Email ) values ( 1, N'Système', '' );

GO

SET IDENTITY_INSERT CK.tActor OFF