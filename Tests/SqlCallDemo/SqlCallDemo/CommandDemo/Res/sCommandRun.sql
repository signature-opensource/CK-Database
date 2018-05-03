-- SetupConfig: {}
create procedure sCommandRun
(
    @ActorId int,
    @CompanyName nvarchar(128),
	@LaunchnDate datetime2(3),
	@Delay int output,
	@ActualCompanyName nvarchar(128) output
)
as
begin
	--[beginsp]

	set @Delay = datediff( second, sysutcdatetime(), @LaunchnDate );
	set @ActualCompanyName = upper(@CompanyName + N' HOP!');

	--[endsp]
end

