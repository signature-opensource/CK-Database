-- Version = 1.0.0
--
-- Deletes a String Data ressource
--
create procedure CK.sResDataStringRemove
	@ResId int
as
begin
	delete from CK.tResDataString where ResId = @ResId;
	return 0;
end

