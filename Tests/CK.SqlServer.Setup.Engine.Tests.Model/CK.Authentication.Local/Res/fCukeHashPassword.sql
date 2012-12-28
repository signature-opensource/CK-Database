-- Version = 1.0.0
-- Computes a hash. @salt is an optional integer value (can be null).
-- SHA1 is currently used: resulting hash is atually 40 characters long.
create function [CK].[fCukeHashPassword]
( 
	@p nvarchar(48), 
	@salt int 
) 
returns varchar(48)
as
begin
	declare @h nvarchar(119); -- select 48*2+10+13
	set @h = @p + isnull(cast(@salt as nvarchar(10)),'Null') + Upper(@p) + 'A bit of salt';
	return SubString(master.dbo.fn_varbintohexstr(HashBytes('SHA1',@h)), 3, 40);
end
