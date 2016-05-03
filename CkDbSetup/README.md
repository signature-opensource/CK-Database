CkDbSetup
=========

CK.Database setup console utility

Purpose
-------

This console app provides a way to setup a SQL Server database from CK.Database project assemblies (through what we call DbSetup), without having to reference the various setup assemblies from inside your project.

Usage and syntax
-----

```
CkDbSetup.exe --help
CkDbSetup.exe --version

CkDbSetup.exe <command> --help
CkDbSetup.exe <command> <...>

CkDbSetup.exe setup <connectionString> <assemblyName> [<assemblyName>...] [options]

CkDbSetup.exe backup <connectionString> <backupFilePath> [options]

CkDbSetup.exe restore <connectionString> <backupFilePath> [options]
```

General options:
- `--help (-h)`: Show the program help.
- `--version (-v)`: Show the program's version.
- `--logFile (-l)`: Path of a log file which will ontain the log output. Defaults to none (console logging only).
- `--logFilter (-f)`: Set a log level filter for console and/or file output.
  - Valid log filters: `Off`, `Release`, `Monitor`, `Terse`, `Verbose`, `Debug`, or any `{Group,Line}` format where Group and Line can be: `Trace`, `Info`, `Warn`, `Error`, `Fatal`, or `Off`.

### `setup` command

```
CkDbSetup.exe setup <connectionString> <assemblyName> [<assemblyName>...] [options]
```
Sets up the given CK.Database assemblies in a SQL Server instance, using the given SQL Server connection string to connect, and generates a structure (`StObjMap`) assembly.
If the database does not exist, the program will try to create it.

Arguments:

- `connectionString`: The SQL Server connection string used to connect to the target database. *It must explicitly contain the target database name (eg. in `Initial Catalog` or `Database`)*. If the database cannot be found in the SQL Server instance, the program will try to create it.
- `assemblyName`: Assembly names to set up in SQL Server.

Options:

- `--help (-h)`: Shows the command help.
- `--binPath (-p)`: *(optional)* Path to the directory containing the assembly files, and in which the generated structure assembly will be saved. Defaults to the current working directory.
- `--generatedAssemblyName (-n)`: Assembly name, and file name (without the `.dll` extension) of the generated structure assembly. Defaults to `CK.StObj.AutoAsssembly`.
- `--generateAssemblyOnly`: Generates the structure assembly without setting up the database.
- 
#### Sample usage

```
CkDbSetup setup "Server=.;Database=MyDatabase;Integrated Security=true;" CK.DB.Basic CK.DB.ACL

CkDbSetup setup "Server=.;Database=MyCofelyDatabase;Integrated Security=true;" CFLY.Data.Setup CK.DB.Actor CK.DB.Zone CK.DB.Acl CK.DB.Location CFLY.Ged CFLY.Ged.Extensions CFLY.OAV --binPath "D:\Dev\Invenietis\Cofely\CFLY.Web\bin" --generatedAssemblyName CFLY.Database.Generated --generateAssemblyOnly
```

### `backup` command

```
CkDbSetup.exe backup <connectionString> <backupFilePath> [options]
```

Creates a full independent, copy-only backup of a SQL Server database to a file on the SQL Server instance.

**<span style="color:red">If the backup file already exists at the given path, it will be overwritten.</span>**

Arguments:

- `connectionString`: The SQL Server connection string used to connect to the database to backup. *It must explicitly contain the name of the database to backup (eg. in `Initial Catalog` or `Database`)*.
  - Note: regardless of the database name given, the backup will be executed while connected to the `master` database.
- `backupFilePath`: The path to the backup file, on the SQL Server host. It can be absolute, or relative to the SQL Server instance's default backup directory.

Options:

- `--help (-h)`: Shows the command help.

### `restore` command

```
CkDbSetup.exe restore <connectionString> <backupFilePath> [options]
```

Restores a SQL Server database from a backup file, automatically moving its data and log files. **Only *simple backup files*, containing one data file and one log file, are supported at this time.**

**<span style="color:red">If the database already exists, it will be overwritten.</span>**

**The data and log files in the backup will be moved to the default data and log directories of the SQL Server instance, to `<databaseName>.mdf` and `<databaseName>_log.ldf` respectively. <span style="color:red">If these files already exist, they will be overwritten. This may cause irreversible loss of any database that uses them.</span>**

Arguments:

- `connectionString`: The SQL Server connection string used to connect to the database name to restore. *It must explicitly contain the name of the database to backup (eg. in `Initial Catalog` or `Database`)*. If the database does not exist, it will be created.
  - Note: regardless of the database name given, the restoration will be executed while connected to the `master` database.
- `backupFilePath`: The path to the backup file to restore, on the SQL Server host. It can be absolute, or relative to the SQL Server instance's default backup directory.


Projected features
------------------

### Clear command

Drop all objects (tables, views, stored procedures) in a given and ordered list of `Schemas` from the database in a `ConnectionString`. This requires the stored procedure `CK.sDropAllSchemaObjects` to exist.

### CkDbSetup configuration file

Allows the `setup`, `clear` and potentially `backup` command to use a configuration file in place of runtime arguments.