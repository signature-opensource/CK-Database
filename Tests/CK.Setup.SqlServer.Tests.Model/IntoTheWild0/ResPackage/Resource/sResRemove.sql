-- Version = 1.0.0
--
-- Removes a ressource
--
create procedure CK.sResRemove
(
	@ResId int
)
as begin
	delete from CK.tRes where ResId = @ResId;
	return 0;
end