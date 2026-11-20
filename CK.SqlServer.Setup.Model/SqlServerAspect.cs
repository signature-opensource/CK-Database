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
/// reads it. An invalid configuration throws.
/// <para>
/// A connection string can be a secret handle (<c>{ "$SECRET": "&lt;Name&gt;" }</c>, see <see cref="ConfigurationSecret"/>):
/// its value is resolved from the development secrets for the setup, and the handle is projected in the hosts (they
/// resolve it when they start).
/// </para>
/// </summary>
[AppBuilderAspect( "SqlServer", TypeActivation.ByDefault )]
public sealed class SqlServerAspect : IAppBuilderAspect
{
    static readonly string[] _keys = ["DefaultDatabaseConnectionString", "Databases", "GlobalResolution", "IgnoreMissingDependencyIsError"];
    static readonly string[] _databaseKeys = ["ConnectionString", "AutoCreate"];

    /// <summary>
    /// Initializes a new <see cref="SqlServerAspect"/> from its configuration.
    /// </summary>
    /// <param name="configuration">The "SqlServer" section. Null when the aspect has no section.</param>
    public SqlServerAspect( IConfiguration? configuration )
    {
        var databases = new List<DatabaseConfiguration>();
        if( configuration != null )
        {
            CheckKeys( configuration, _keys, "" );
            (DefaultDatabaseConnectionString, DefaultDatabaseConnectionStringSecret) = ReadConnectionString( configuration, "DefaultDatabaseConnectionString", "" );
            // The origin of the projected value: a "$LOCK" on it burns the value instead of projecting it.
            if( configuration is IConfigurationSection originSection ) _connectionStringOrigin = $"{originSection.Path}:DefaultDatabaseConnectionString";
            GlobalResolution = ReadBool( configuration, "GlobalResolution" );
            IgnoreMissingDependencyIsError = ReadBool( configuration, "IgnoreMissingDependencyIsError" );
            foreach( var d in configuration.GetSection( "Databases" ).GetChildren() )
            {
                CheckKeys( d, _databaseKeys, $"Databases:{d.Key}:" );
                var (dbCs, dbSecret) = ReadConnectionString( d, "ConnectionString", $"Databases:{d.Key}:" );
                if( dbCs == null && dbSecret == null ) Throw.CKException( $"\"Databases:{d.Key}:ConnectionString\" is required." );
                if( d.Key == SqlSetupAspectConfiguration.DefaultDatabaseName )
                {
                    Throw.CKException( $"The database '{d.Key}' is the default one: its connection string is \"DefaultDatabaseConnectionString\"." );
                }
                databases.Add( new DatabaseConfiguration( d.Key, dbCs, dbSecret, ReadBool( d, "AutoCreate" ) ) );
            }
        }
        Databases = databases;

        static void CheckKeys( IConfiguration c, string[] keys, string prefix )
        {
            foreach( var k in c.GetChildren() )
            {
                if( !keys.Contains( k.Key, StringComparer.OrdinalIgnoreCase ) )
                {
                    Throw.CKException( $"Unknown \"{prefix}{k.Key}\": the keys are {keys.Concatenate()}." );
                }
            }
        }

        // A plain value, or the name of a secret ({ "$SECRET": "<Name>" }).
        static (string? Value, string? Secret) ReadConnectionString( IConfiguration c, string key, string prefix )
        {
            var v = c[key];
            if( !string.IsNullOrWhiteSpace( v ) ) return (v, null);
            var s = c.GetSection( key );
            var secret = s[ConfigurationSecret.HandleKey];
            if( secret == null ) return (null, null);
            if( !ConfigurationSecret.IsValidName( secret ) || s.GetChildren().Skip( 1 ).Any() )
            {
                Throw.CKException( $"Invalid \"{prefix}{key}\": a connection string or a secret handle {{ \"{ConfigurationSecret.HandleKey}\": \"<Name>\" }} is expected." );
            }
            return (null, secret);
        }

        static bool ReadBool( IConfiguration c, string key )
        {
            var v = c[key];
            if( v == null ) return false;
            if( !bool.TryParse( v, out var b ) ) Throw.CKException( $"Invalid \"{key}\": '{v}' (true or false is expected)." );
            return b;
        }
    }

    /// <summary>
    /// Gets the connection string of the default database ("db"). Null when not configured or when it is a secret.
    /// </summary>
    public string? DefaultDatabaseConnectionString { get; }

    /// <summary>
    /// Gets the name of the secret that is the connection string of the default database. Null when it is not a secret.
    /// </summary>
    public string? DefaultDatabaseConnectionStringSecret { get; }

    readonly string? _connectionStringOrigin;

    /// <summary>
    /// Gets the other databases.
    /// </summary>
    public IReadOnlyList<DatabaseConfiguration> Databases { get; }

    /// <summary>
    /// The configuration of another database: its connection string is a value or the name of a secret.
    /// </summary>
    /// <param name="LogicalDatabaseName">The logical name of the database.</param>
    /// <param name="ConnectionString">The connection string. Null when it is a secret.</param>
    /// <param name="ConnectionStringSecret">The name of the secret that is the connection string. Null when it is not a secret.</param>
    /// <param name="AutoCreate">Whether the database is created when it doesn't exist.</param>
    public sealed record DatabaseConfiguration( string LogicalDatabaseName, string? ConnectionString, string? ConnectionStringSecret, bool AutoCreate );

    /// <summary>
    /// Gets <see cref="SqlSetupAspectConfiguration.GlobalResolution"/>.
    /// </summary>
    public bool GlobalResolution { get; }

    /// <summary>
    /// Gets <see cref="SqlSetupAspectConfiguration.IgnoreMissingDependencyIsError"/>.
    /// </summary>
    public bool IgnoreMissingDependencyIsError { get; }

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
        var defaultConnectionString = DefaultDatabaseConnectionString;
        if( DefaultDatabaseConnectionStringSecret != null && !context.TryResolveSecret( monitor, DefaultDatabaseConnectionStringSecret, out defaultConnectionString ) )
        {
            return false;
        }
        if( defaultConnectionString != null ) sql.DefaultDatabaseConnectionString = defaultConnectionString;
        foreach( var d in Databases )
        {
            var cs = d.ConnectionString;
            if( d.ConnectionStringSecret != null && !context.TryResolveSecret( monitor, d.ConnectionStringSecret, out cs ) ) return false;
            sql.Databases.RemoveAll( x => x.LogicalDatabaseName == d.LogicalDatabaseName );
            sql.Databases.Add( new SqlDatabaseDescriptor( d.LogicalDatabaseName, cs!, d.AutoCreate ) );
        }
        sql.GlobalResolution |= GlobalResolution;
        sql.IgnoreMissingDependencyIsError |= IgnoreMissingDependencyIsError;
        if( sql.DefaultDatabaseConnectionString == null )
        {
            monitor.Error( $"The \"SqlServer\" aspect has no \"DefaultDatabaseConnectionString\" (in the \"Aspects\" of the builder): {apps.Select( a => a.ToString() ).Concatenate()} need a database." );
            return false;
        }
        // The value configured here (not one of a "CKSetup.xml") is projected from its origin: a "$LOCK" on it applies.
        bool fromHere = _connectionStringOrigin != null && sql.DefaultDatabaseConnectionString == defaultConnectionString;
        if( fromHere && DefaultDatabaseConnectionStringSecret == null )
        {
            // The development configuration accepts it (an Integrated Security connection string).
            monitor.Info( $"\"{_connectionStringOrigin}\" is a secret (the \"SqlServer\" aspect) whose value is in the configuration: a handle keeps it out of the files ({{ \"{ConfigurationSecret.HandleKey}\": \"<Name>\" }})." );
        }
        foreach( var a in apps )
        {
            if( fromHere && DefaultDatabaseConnectionStringSecret != null )
            {
                // The hosts resolve the secret when they start.
                a.ProjectValue( $"SqlServer:DefaultDatabaseConnectionString:{ConfigurationSecret.HandleKey}",
                                DefaultDatabaseConnectionStringSecret,
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
