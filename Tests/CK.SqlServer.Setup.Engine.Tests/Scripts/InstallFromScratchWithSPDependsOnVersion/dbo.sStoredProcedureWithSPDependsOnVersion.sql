-- Version = 1.0.1, Package = WithSPDependsOnVersion
create procedure dbo.sStoredProcedureWithSPDependsOnVersion
(
	@P int
)
as
begin
	--[beginsp]
	select Id, Id2 from dbo.tTestVSP;
	--[endsp]
end