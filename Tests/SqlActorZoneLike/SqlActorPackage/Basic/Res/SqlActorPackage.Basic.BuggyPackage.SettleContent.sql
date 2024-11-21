
--[beginscript]

if object_id('CK.tBuggyPackageSettleContent') is null
    create table CK.tBuggyPackageSettleContent
    (
	    Id int not null identity (0, 1),
        SetupTime DateTime not null 
    );

insert into CK.tBuggyPackageSettleContent(SetupTime)
   select LastStartDate from CKCore.tSetupMemory where SurrogateId = 0;

--[endscript]
