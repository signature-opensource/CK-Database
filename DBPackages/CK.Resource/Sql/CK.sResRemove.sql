-- Version = 1.0.0, Package = CK.Resource
--
-- Removes a ressource
--
create procedure CK.sResRemove
(
	@ActorId	int,
	@ResId		int
)
as begin
		
	set nocount on;
	
	delete from CK.tRes where ResId = @ResId;
	return 0;
	
end