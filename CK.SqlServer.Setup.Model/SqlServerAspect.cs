using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Setup;

/// <summary>
/// The "SqlServer" Builder aspect: the setup of the SQL Server databases, engine wide (one per engine run, shared by the
/// Apps of a builder). Present as soon as an App references this model; its section in the root <c>"Aspects"</c> of the
/// builder's "ckonf.json":
/// <code>
/// "Aspects": {
///   "SqlServer": {
///     "DefaultDatabaseConnectionString": "Server=.;Database=MyDB;Integrated Security=True;TrustServerCertificate=true",
///     "Databases": { "histoDB": { "ConnectionString": "...", "AutoCreate": true } },  // Optional: the other databases.
///     "GlobalResolution": false,                                                     // Optional.
///     "IgnoreMissingDependencyIsError": false                                        // Optional.
///   }
/// }
/// </code>
/// It configures the engine aspects of the 3 steps setup and of SQL Server (<see cref="SetupableAspectConfiguration"/> with
/// its defaults, <see cref="SqlSetupAspectConfiguration"/>) and projects <c>"SqlServer": { "DefaultDatabaseConnectionString" }</c>
/// in the "appsettings.json" of the hosts of the Apps: the run time default database (<see cref="SqlDefaultDatabase"/>)
/// reads it. Its configuration is a model (<see cref="SqlServerConfiguration"/>).
/// <para>
/// A connection string can be a secret handle (<c>{ "$SECRET": "&lt;Name&gt;" }</c>, see <see cref="ConfigurationSecret"/>):
/// its value is resolved from the development secrets for the setup, and the handle is projected in the hosts (they
/// resolve it when they start).
/// </para>
/// </summary>
[AppBuilderAspect( "SqlServer", TypeActivation.ByDefault )]
[Configuration<SqlServerConfiguration>]
public sealed class SqlServerAspect : IAppBuilderAspectApply
{
    readonly SqlServerConfiguration _configuration;
    readonly string? _connectionStringOrigin;

    /// <summary>
    /// Initializes a new <see cref="SqlServerAspect"/> from its configuration.
    /// </summary>
    /// <param name="configuration">The (checked) configuration.</param>
    /// <param name="section">The "SqlServer" section: the origin of the projected connection string. Null when the aspect has no section.</param>
    public SqlServerAspect( SqlServerConfiguration configuration, IConfiguration? section )
    {
        _configuration = configuration;
        // The origin of the projected value: a "$LOCK" on it burns the value instead of projecting it.
        if( section is IConfigurationSection s ) _connectionStringOrigin = $"{s.Path}:DefaultDatabaseConnectionString";
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public SqlServerConfiguration Configuration => _configuration;

    /// <inheritdoc />
    public bool Apply( IActivityMonitor monitor, AppBuilderContext context, IReadOnlyList<AppConfiguration> apps )
    {
        // The setup of the database is done by the build (the legacy SQL layer can't generate the scripts): the build of a
        // deployment can't do it (its database is not the one of the build machine, and a build never changes it).
        if( context.Deployment != null )
        {
            monitor.Error( $"The database of the deployment '{context.Deployment.Name}' can't be set up by its build: the SqlServer aspect sets up the database at build time (the SQL layer can't generate the scripts yet). Leave this builder out of the deployment (\"Deployment\": {{ \"Builders\": [...] }})." );
            return false;
        }
        // The 3 steps setup is an implementation detail of SQL Server: it has no configuration of its own.
        context.EngineConfiguration.EnsureAspect<SetupableAspectConfiguration>();
        var sql = context.EngineConfiguration.EnsureAspect<SqlSetupAspectConfiguration>();
        // The setup needs the values: a secret is resolved from the development secrets.
        var defaultSecret = _configuration.DefaultDatabaseConnectionString?.SecretName;
        var defaultConnectionString = _configuration.DefaultDatabaseConnectionString?.Value;
        if( defaultSecret != null && !context.TryResolveSecret( monitor, defaultSecret, out defaultConnectionString ) )
        {
            return false;
        }
        if( defaultConnectionString != null ) sql.DefaultDatabaseConnectionString = defaultConnectionString;
        foreach( var (name, d) in _configuration.Databases )
        {
            var cs = d.ConnectionString.Value;
            if( d.ConnectionString.SecretName is string secret && !context.TryResolveSecret( monitor, secret, out cs ) ) return false;
            sql.Databases.RemoveAll( x => x.LogicalDatabaseName == name );
            sql.Databases.Add( new SqlDatabaseDescriptor( name, cs!, d.AutoCreate ) );
        }
        sql.GlobalResolution |= _configuration.GlobalResolution;
        sql.IgnoreMissingDependencyIsError |= _configuration.IgnoreMissingDependencyIsError;
        if( sql.DefaultDatabaseConnectionString == null )
        {
            monitor.Error( $"The \"SqlServer\" aspect has no \"DefaultDatabaseConnectionString\" (in the \"Aspects\" of the builder): {apps.Select( a => a.ToString() ).Concatenate()} need a database." );
            return false;
        }
        // The value configured here (not one of a "CKSetup.xml") is projected from its origin: a "$LOCK" on it applies.
        bool fromHere = _connectionStringOrigin != null && sql.DefaultDatabaseConnectionString == defaultConnectionString;
        if( fromHere && defaultSecret == null )
        {
            // The development configuration accepts it (an Integrated Security connection string).
            monitor.Info( $"\"{_connectionStringOrigin}\" is a secret (the \"SqlServer\" aspect) whose value is in the configuration: a handle keeps it out of the files ({{ \"{ConfigurationSecret.HandleKey}\": \"<Name>\" }})." );
        }
        foreach( var a in apps )
        {
            if( fromHere && defaultSecret != null )
            {
                // The hosts resolve the secret when they start.
                a.ProjectValue( $"SqlServer:DefaultDatabaseConnectionString:{ConfigurationSecret.HandleKey}",
                                defaultSecret,
                                $"{_connectionStringOrigin}:{ConfigurationSecret.HandleKey}" );
            }
            else if( fromHere )
            {
                a.ProjectValue( "SqlServer:DefaultDatabaseConnectionString", sql.DefaultDatabaseConnectionString, _connectionStringOrigin! );
            }
            else
            {
                a.ProjectValue( "SqlServer:DefaultDatabaseConnectionString", sql.DefaultDatabaseConnectionString );
            }
        }
        return true;
    }
}
