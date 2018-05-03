-- SetupConfig: {}
create procedure CK.sPurelyInputSimpleLog
(
	@LogText nvarchar(250)
)
as
begin
	--<beginsp>	
	if object_id('CK.tPurelyInputLog') is null
	begin
		create table CK.tPurelyInputLog( Id int identity(1,1) primary key, CreationDate DateTime2, LogText nvarchar(300) ); 
	end
	insert into CK.tPurelyInputLog( CreationDate, LogText ) values( sysutcdatetime(), @LogText + ' - SimpleLog' );
	--<endsp>
	return 0;
end

