-- SetupConfig: {}
create view ITrans.vBase
as
    select KeyValue = object_id from sys.tables;

