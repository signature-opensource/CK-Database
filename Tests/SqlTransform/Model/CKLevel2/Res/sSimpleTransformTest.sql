-- Version = *
create transformer on CK.sSimpleTransformTest
as
begin
	add parameter @Added int = 0;
	replace single {N'No!'} with "N'Yes! ' + cast(@Added as varchar)";
end

