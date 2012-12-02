
create table CK.tGroup
(
  GroupId int not null,
  GroupName varchar(32) not null,
  CreationDate datetime not null constraint DF_tGroup_CreationDate default( getutcdate() ),

  constraint PK_tGroup primary key clustered( GroupId ),
  constraint FK_tGroup_ActorId foreign key ( GroupId ) references CK.tActor( ActorId ),
  constraint UK_tGroup_GroupName unique( GroupName )

);
--
insert into CK.tGroup( GroupId, GroupName ) values( 0, 'Public' );
insert into CK.tGroup( GroupId, GroupName ) values( 1, 'System' );

