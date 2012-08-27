-- Version = 1.0.0, Package = Test
create procedure Test.sStoredProcedureB
(
	@P int
)
as
begin
	--[beginsp]
	select P = @P
	--[endsp]
end
