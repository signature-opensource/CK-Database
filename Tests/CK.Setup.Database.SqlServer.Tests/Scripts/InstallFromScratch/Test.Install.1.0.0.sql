if not exists(select 1 from sys.schemas where name = 'Test')
begin
    exec( 'create schema Test' );
end
