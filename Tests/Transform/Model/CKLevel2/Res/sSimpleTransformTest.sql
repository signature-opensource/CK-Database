-- Version = *
create transformer on CK.sSimpleTransformTest
as
begin
	replace single {N'No!'} with "N'Yes!'";
end

