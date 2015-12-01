if not exists(select 1 from sys.schemas where name = 'CK')
begin
	exec( 'create schema CK' );
end
go
if object_id('CK.sStupidTest') is not null drop procedure CK.sStupidTest;
go
create procedure CK.sStupidTest
	@x int, 
	@y int output, 
	@d DateTime output, 
	@s nvarchar(64) output,
	@z int
as
begin
	set @s = 'x=' + cast( @x as nvarchar ) +' z=' + cast( @z as nvarchar );
	set @y = @x + @z;
	set @d = getutcdate();  
	return 0;
end

