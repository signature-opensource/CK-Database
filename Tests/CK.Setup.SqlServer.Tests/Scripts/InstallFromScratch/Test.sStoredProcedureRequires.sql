-- Version = 1.0.0, Requires={Test.sOneStoredProcedure, Test.sOneStoredProcedureA, Test.udtMyTestType}
create procedure Test.sStoredProcedureRequires
(
	@P int
)
as
begin
	--[beginsp]
	select P = @P;
	--[endsp]
end
