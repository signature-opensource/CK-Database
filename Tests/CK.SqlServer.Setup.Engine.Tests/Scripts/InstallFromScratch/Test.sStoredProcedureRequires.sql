-- Version = 1.0.0, Requires={Test.sOneStoredProcedure, Test.sOneStoredProcedureA}
create procedure Test.sStoredProcedureRequires
(
	@P int
)
as
begin
	--[beginsp]
	select P = @P;
	exec Test.MissingProc;
	--[endsp]
end