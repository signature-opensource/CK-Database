-- SetupConfig : {}
create procedure CK.sBasicSimpleScalar
(
	@Index	int,
	@Name	varchar(256)
)
as
begin
	select @Name + ' - ' + Cast( @Index as varchar );
	return 0;
end

