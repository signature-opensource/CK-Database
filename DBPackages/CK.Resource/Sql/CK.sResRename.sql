-- Version = 1.0.0, Package = CK.Resource
--
-- Renames all @Old(Res)Name by @New(Res)Name
-- Renames also the chidren of the resource name.
--
create procedure CK.sResRename
(
	@ActorId	int,
	@ResId		int,
	@NewName	varchar(96)
)
as
begin
	declare @oldName varchar(96);

	select @oldName = ResName from CK.tRes where ResId = @ResId;
	if @oldName is not null
	begin

		set @NewName = RTrim( LTrim(@NewName) );

		-- update child names first
		declare @lenPrefix int;
		set @lenPrefix = len(@oldName)+1;
		if len(@NewName) = 0 
		begin
			set @lenPrefix = @lenPrefix + 1;
		end

		update CK.tRes set ResName = @NewName+substring( ResName, @lenPrefix, 96 )
			where ResName like @oldName+'%';

	end

	return 0;

end