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
```

General options:
- `--help (-h)`: Shows the program help.
- `--version (-v)`: Shows the program's version.


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

#### Sample usage
```
CkDbSetup setup "Server=.;Database=MyDatabase;Integrated Security=true;" CK.DB.Basic CK.DB.ACL

CkDbSetup setup "Server=.;Database=MyCofelyDatabase;Integrated Security=true;" CFLY.Data.Setup CK.DB.Actor CK.DB.Zone CK.DB.Acl CK.DB.Location CFLY.Ged CFLY.Ged.Extensions CFLY.OAV --binPath "D:\Dev\Invenietis\Cofely\CFLY.Web\bin" --generatedAssemblyName CFLY.Database.Generated
```


Projected features
------------------

### Backup command

Creates an independent copy-only full backup from the database in a `ConnectionString` to a single file.

### Clear command

Drop all objects (tables, views, stored procedures) in a given and ordered list of `Schemas` from the database in a `ConnectionString`. This requires the stored procedure `CK.sDropAllSchemaObjects` to exist.

### CkDbSetup configuration file

Allows the `setup`, `clear` and potentially `backup` command to use a configuration file in place of runtime arguments.