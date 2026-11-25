using CK.Core;
using System.Collections.Generic;

namespace CK.Setup;

/// <summary>
/// The configuration of the "SqlServer" Builder aspect (see <see cref="SqlServerAspect"/>). The connection strings are
/// secrets: their configuration should be a handle (<c>{ "$SECRET": "&lt;Name&gt;" }</c>), resolved from the development
/// secrets for the setup.
/// </summary>
[ConfigurationModel]
public sealed class SqlServerConfiguration : IConfigurationModel
{
    /// <summary>
    /// Gets the connection string of the default database ("db").
    /// </summary>
    [ConfigurationSecret]
    public ConfigurationSecretValue? DefaultDatabaseConnectionString { get; init; }

    /// <summary>
    /// Gets the other databases, by logical name ("db" is the default one: it can't be one of them).
    /// </summary>
    public IReadOnlyDictionary<string, SqlServerDatabaseConfiguration> Databases { get; init; } = new Dictionary<string, SqlServerDatabaseConfiguration>();

    /// <summary>
    /// Gets <see cref="SqlSetupAspectConfiguration.GlobalResolution"/>.
    /// </summary>
    public bool GlobalResolution { get; init; }

    /// <summary>
    /// Gets <see cref="SqlSetupAspectConfiguration.IgnoreMissingDependencyIsError"/>.
    /// </summary>
    public bool IgnoreMissingDependencyIsError { get; init; }

    /// <inheritdoc />
    public void Validate( IConfigurationValidationContext context )
    {
        if( DefaultDatabaseConnectionString is { IsHandle: false } v && string.IsNullOrWhiteSpace( v.Value ) )
        {
            context.Error( $"'{context.Path}:DefaultDatabaseConnectionString' must not be empty." );
        }
        foreach( var name in Databases.Keys )
        {
            if( name == SqlSetupAspectConfiguration.DefaultDatabaseName )
            {
                context.Error( $"'{context.Path}:Databases:{name}': the database '{name}' is the default one, its connection string is \"DefaultDatabaseConnectionString\"." );
            }
        }
    }
}

/// <summary>
/// The configuration of a database of the "SqlServer" Builder aspect other than the default one.
/// </summary>
[ConfigurationModel]
public sealed class SqlServerDatabaseConfiguration : IConfigurationModel
{
    /// <summary>
    /// Gets the connection string of the database.
    /// </summary>
    [ConfigurationSecret]
    public required ConfigurationSecretValue ConnectionString { get; init; }

    /// <summary>
    /// Gets whether the database is created when it doesn't exist.
    /// </summary>
    public bool AutoCreate { get; init; }

    /// <inheritdoc />
    public void Validate( IConfigurationValidationContext context )
    {
        if( !ConnectionString.IsHandle && string.IsNullOrWhiteSpace( ConnectionString.Value ) )
        {
            context.Error( $"'{context.Path}:ConnectionString' must not be empty." );
        }
    }
}
