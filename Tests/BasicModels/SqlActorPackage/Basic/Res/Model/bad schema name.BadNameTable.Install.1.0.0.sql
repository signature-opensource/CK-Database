--[beginscript]

create table [bad schema name].[bad name table]
(
	Id int not null identity (0, 1),
    Place varchar(50) not null,
    TrancountTest int not null
);

declare @TC int = @@TRANCOUNT;
insert into [bad schema name].[bad name table]( Place, TrancountTest ) values( 'Initial', @TC );

--[endscript]

-- This executes outside any [begin/endscript] scope.
declare @TC int = @@TRANCOUNT;
insert into [bad schema name].[bad name table]( Place, TrancountTest ) values( 'Out of [begin/endscript]', @TC );

--[beginscript]

declare @TC int = @@TRANCOUNT;
insert into [bad schema name].[bad name table]( Place, TrancountTest ) values( 'In subsequent [begin/endscript]', @TC );

--[endscript]

