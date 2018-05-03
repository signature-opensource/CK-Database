-- SetupConfig: {}
create procedure sActorOnly
(
    @ActorId int,
	@TextResult nvarchar(128) output
)
as
begin
	if @ActorId is null set @TextResult = N'@ActorId is null';
	else set @TextResult = N'@ActorId = ' + cast( @ActorId as nvarchar );
	return 0;
end

