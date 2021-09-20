-- SetupConfig: {}
create procedure CK.sPocoThingRead
( 
	@Name varchar(50) output,
    @UniqueId uniqueidentifier output
)
as
begin
	set @Name = 'ReadFromDatabase';
    set @UniqueId = newid();
	return 0;
end

