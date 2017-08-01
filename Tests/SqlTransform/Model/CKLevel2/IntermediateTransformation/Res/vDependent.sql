-- SetupConfig: { "Requires": "CKLevel2.IntermediateTransformation.Package2(ITrans.vBase)" }
create view ITrans.vDependent
as
    select I = KeyValue, N = [name]
        from ITrans.vBase;

