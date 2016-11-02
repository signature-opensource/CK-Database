--[beginscript]

create table [bad schema name].[bad name table]
(
	Id int not null identity (0, 1),
);

insert into [bad schema name].[bad name table] default values;

--[endscript]
