-- Version = 1.0.10
alter procedure CK.sAllDefaultValues
(
	@NVarChar nvarchar(64) = N'All Defaults',
	@Int int = 3712,
	@BigInt bigint = 9223372036854775807,
	@SmallInt smallint = -32768,
	@TinyInt tinyint = 255,
	@Bit bit = 1,
	@Numeric numeric = 123456789012345678,
	@Numeric2010 numeric(20,10) = 1234567890.0123456789,
	@DateTime datetime = '2011-10-26',
	@Float float = -457.5858e-8,
	@Real real = -45.588e-10,
	@TextResult nvarchar(1024) output
)
as
begin
	if @NVarChar is null set @TextResult = '@NVarChar is null'; else set @TextResult = '@NVarChar = ' + @NVarChar;
	if @Int is null set @TextResult = @TextResult + ' - @Int is null'; else set @TextResult = @TextResult + ' - @Int = ' + cast( @Int as nvarchar); 
	if @BigInt is null set @TextResult = @TextResult + ' - @BigInt is null'; else set @TextResult = @TextResult + ' - @BigInt = ' + cast( @BigInt as nvarchar); 
	if @SmallInt is null set @TextResult = @TextResult + ' - @SmallInt is null'; else set @TextResult = @TextResult + ' - @SmallInt = ' + cast( @SmallInt as nvarchar); 
	if @SmallInt is null set @TextResult = @TextResult + ' - @TinyInt is null'; else set @TextResult = @TextResult + ' - @TinyInt = ' + cast( @TinyInt as nvarchar); 
	if @Bit is null set @TextResult = @TextResult + ' - @Bit is null'; else set @TextResult = @TextResult + ' - @Bit = ' + cast( @Bit as nvarchar); 
	if @Numeric is null set @TextResult = @TextResult + ' - @Numeric is null'; else set @TextResult = @TextResult + ' - @Numeric = ' + cast( @Numeric as nvarchar(50)); 
	if @Numeric2010 is null set @TextResult = @TextResult + ' - @Numeric2010 is null'; else set @TextResult = @TextResult + ' - @Numeric2010 = ' + cast( @Numeric2010 as nvarchar(50)); 
	if @DateTime is null set @TextResult = @TextResult + ' - @DateTime is null'; else set @TextResult = @TextResult + ' - @DateTime = ' + cast( @DateTime as nvarchar(50)); 
	if @Float is null set @TextResult = @TextResult + ' - @Float is null'; else set @TextResult = @TextResult + ' - @Float = ' + cast( @Float as nvarchar(50)); 
	if @Real is null set @TextResult = @TextResult + ' - @Real is null'; else set @TextResult = @TextResult + ' - @Real = ' + cast( @Real as nvarchar(50)); 
	return 0;
end

