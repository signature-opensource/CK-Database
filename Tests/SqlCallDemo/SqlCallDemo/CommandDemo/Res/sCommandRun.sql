-- SetupConfig: {}
create procedure sCommandRun
(
    @ActorId int,
    @CompanyName nvarchar(128),
	@LaunchDate datetime2(2),
	@Delay int output,
	@ActualCompanyName nvarchar(128) output
)
as
begin
	--[beginsp]

	set @Delay = datediff( second, sysutcdatetime(), @LaunchDate );
	set @ActualCompanyName = upper(@CompanyName + N' HOP!');

	--[endsp]
end

