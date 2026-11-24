NUnit test helpers for the SQL Server packages: the referencing test project runs the shared engine with the SqlServer aspect and the Cris execution context.

Main types: `SqlServerConfigurationAspectAndCrisExecutionContext` (the assembly attribute added automatically), `SqlDatabaseExtensions` (`TemporaryTransform`, `GetCKCoreInvariantsViolations`...), `SimpleDataTable` and the `DBSetup` fixture of interactive tests (drop the database, backups).
