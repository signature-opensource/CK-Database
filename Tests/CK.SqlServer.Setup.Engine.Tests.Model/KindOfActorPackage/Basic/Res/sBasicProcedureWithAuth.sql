-- SetupConfig : {}
create procedure CK.sBasicProcedureWithAuth 
(
	@ActorId	int,
	@Index		int,
	@Name		varchar(256),
	@Result		varchar(512) output
)
as
begin
	set @Result = Cast( @ActorId as varchar ) + ': ' + @Name + ' - ' + Cast( @Index as varchar );
	return 0;
end

