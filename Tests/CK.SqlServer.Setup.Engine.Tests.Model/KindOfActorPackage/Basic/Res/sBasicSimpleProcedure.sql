-- Version = 1.0.0
create procedure CK.sBasicSimpleProcedure 
(
	@Index	int,
	@Name	varchar(256),
	@Result	varchar(512) output
)
as
begin
	set @Result = @Name + ' - ' + Cast( @Index as varchar );
	return 0;
end

