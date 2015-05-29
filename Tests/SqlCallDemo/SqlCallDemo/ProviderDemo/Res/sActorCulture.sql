-- Version = 1.0.0
create procedure sActorCulture
(
    @ActorId int,
    @CultureId int,
	@TextResult nvarchar(128) output
)
as
begin

	if @ActorId is null set @TextResult = N'@ActorId is null';
	else set @TextResult = N'@ActorId = ' + cast( @ActorId as nvarchar );

	if @CultureId is null set @TextResult = @TextResult + N', @CultureId is null';
	else set @TextResult = @TextResult + N', @CultureId = ' + cast( @CultureId as nvarchar );

	return 0;
end

