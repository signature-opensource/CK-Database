-- Version = 3.7.3
create procedure CK.sIntReturnWithActor
(
    @ActorId int,
	@Def nvarchar(64) = N'5',
	@Result int output
)
as
begin
	set @Result = @ActorId * @ActorId * cast( @Def as int);
	return 0;
end

