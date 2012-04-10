-- Version = 1.0.0, Package = CK.Resource
--
-- Renames all @Old(Res)Name by @New(Res)Name
-- Renames also the chidren of the resource name.
--
create procedure CK.sResRename
	@OldName	varchar(96),
	@NewName	varchar(96)
as
begin
	if @OldName = '' return 0;
	set @NewName = RTrim( LTrim(@NewName) );
	
	-- update child names first
	declare @lenPrefix int;
	set @lenPrefix = len(@OldName)+1;
	if len(@NewName) = 0 set @lenPrefix = @lenPrefix + 1;
	update CK.tRes set ResName = @NewName+substring( ResName, @lenPrefix, 96 )
		where ResName like @OldName+'%';

	return 0;

end