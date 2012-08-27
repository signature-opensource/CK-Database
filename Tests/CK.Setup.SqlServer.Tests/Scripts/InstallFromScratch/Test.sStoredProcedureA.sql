-- Version = 1.0.0, Package = Test, Requires={Test.sStoredProcedureB, Test.sStoredProcedureC}
create procedure Test.sStoredProcedureA
(
	@P int
)
as
begin
	--[beginsp]
	select P = @P
	--[endsp]
end
