-- Version = 3.7.1
create procedure CK.sIntReturnWithActor
(
    @ActorId int,
	@Result int output
)
as
begin
	set @Result = @ActorId * @ActorId;
	return 0;
end

