-- SetupConfig: {}
create procedure CK.sPocoThingWrite
( 
	@Name varchar(50),
	@Result varchar(800) output 
)
as
begin
	set @Result = @Name;
	return 0;
end

