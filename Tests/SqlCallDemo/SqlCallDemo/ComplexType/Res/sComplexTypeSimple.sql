-- Version = 1.0.10
alter procedure CK.sComplexTypeSimple
(
	@Id int = 0 /*input*/output,
	@Name nvarchar(50) = N'The name...' /*input*/output,
	@CreationDate datetime = '2015-06-03' /*input*/output,
	@NullableInt int output
)
as
begin
	if @Id = 0 set @NullableInt = null;
	else set @NullableInt = @Id;
	set @Id = @Id * 3712;
	set @Name = @Name + cast( @Id as nvarchar );
	set @CreationDate = getutcdate();
	return 0;
end

