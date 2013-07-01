-- Version = 1.0.0
--
-- Sets a String Data ressource
--
create procedure CK.sResDataStringSet
(
	@ResId int, 
	@Val nvarchar(400)
)
as begin
	set nocount on;
	merge CK.tResDataString as t 
		using (select ResId = @ResId, Val = @Val) as s
		on t.ResId = s.ResId
		when matched then update set Val = @Val
		when not matched then insert(ResId,Val) values (@ResId, @Val);
	return 0;
end