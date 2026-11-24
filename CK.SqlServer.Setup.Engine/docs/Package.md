The engine aspect of the SQL Server setup: `SqlSetupAspect` sets up the SQL Server databases of the application, with `SqlManager` that executes the scripts and can create a missing database.

It also stores the versions of the setup items in the database (`SqlVersionedItemReader`, `SqlVersionedItemWriter`) and generates the IPoco registration of CK.SqlServer.Dapper.
