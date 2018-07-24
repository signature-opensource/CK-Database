-- SetupConfig: {}
create procedure CK.sPocoThingRead
( 
	@Name varchar(50) output,
    @FromBatabaseOnly uniqueidentifier output
)
as
begin
	set @Name = 'ReadFromDatabase';
    set @FromBatabaseOnly = newid();
	return 0;
end

