-- Version = 1.0.0
create procedure Test.sOneStoredProcedureA
(
	@P int
)
as
begin
	--[beginsp]
	select P = @P;
	--[endsp]
end