--[beginscript]

create table CK.tActor
(
	ActorId int not null identity (0, 1),
	constraint PK_tActor primary key nonclustered (ActorId)
);

-- Anonymous.
insert into CK.tActor default values;
-- System.
insert into CK.tActor default values;

--[endscript]
