-- Version = 1.0.0, Package = Test
create procedure Test.sStoredProcedureC
(
	@P int
)
as
begin
	--[beginsp]
	select P = @P
	--[endsp]
end
