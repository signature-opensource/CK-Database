-- SetupConfig: {}
alter procedure CK.sPurelyInputLog
(
    @OneMore bit = 1,
	@LogText nvarchar(250),
	@WaitTimeMS int = 0
)
as
begin
	--<beginsp>	
	if object_id('CK.tPurelyInputLog') is null
	begin
		create table CK.tPurelyInputLog( Id int identity(1,1) primary key, CreationDate DateTime2, LogText nvarchar(300) ); 
	end

	if @OneMore is null set @LogText = @LogText + N' - @OneMore is null';
	if @OneMore = 1 set @LogText = @LogText + N' - @OneMore = 1';
	if @OneMore = 0 set @LogText = @LogText + N' - @OneMore = 0';

	if @WaitTimeMS > 0 
	begin
		declare @now datetime = getdate();
		set @now = dateadd( ms, @WaitTimeMS, @now );
		WaitFor time @now; 
	end 
	insert into CK.tPurelyInputLog( CreationDate, LogText ) values( sysutcdatetime(), @LogText );

	--<endsp>
	return 0;
end
