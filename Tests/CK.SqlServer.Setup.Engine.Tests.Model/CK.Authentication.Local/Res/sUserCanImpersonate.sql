-- SetupConfig: {}
-- sUserCanImpersonate 
create procedure CK.sUserCanImpersonate
(
    @ActorId int,
	@AuthenticatedActorId int,
	@ImpersonatedUserId int,
    @CanImpersonate bit output
)
as begin

	set nocount on;

	--<PreCanImpersonate />

	if @AuthenticatedActorId = 1 -- Only god can judge me
	begin
		set @CanImpersonate = 1;
	end
	else
	begin
		set @CanImpersonate = 0;
	end

	--<PostCanImpersonate />
end