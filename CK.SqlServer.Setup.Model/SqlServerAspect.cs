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
        var databases = new List<SqlDatabaseDescriptor>();
        if( configuration != null )
        {
            CheckKeys( configuration, _keys, "" );
            var cs = configuration["DefaultDatabaseConnectionString"];
            DefaultDatabaseConnectionString = string.IsNullOrWhiteSpace( cs ) ? null : cs;
            GlobalResolution = ReadBool( configuration, "GlobalResolution" );
            IgnoreMissingDependencyIsError = ReadBool( configuration, "IgnoreMissingDependencyIsError" );
            foreach( var d in configuration.GetSection( "Databases" ).GetChildren() )
            {
                CheckKeys( d, _databaseKeys, $"Databases:{d.Key}:" );
                var dbCs = d["ConnectionString"];
                if( string.IsNullOrWhiteSpace( dbCs ) ) Throw.CKException( $"\"Databases:{d.Key}:ConnectionString\" is required." );
                if( d.Key == SqlSetupAspectConfiguration.DefaultDatabaseName )
                {
                    Throw.CKException( $"The database '{d.Key}' is the default one: its connection string is \"DefaultDatabaseConnectionString\"." );
                }
                databases.Add( new SqlDatabaseDescriptor( d.Key, dbCs, ReadBool( d, "AutoCreate" ) ) );
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

        static bool ReadBool( IConfiguration c, string key )
        {
            var v = c[key];
            if( v == null ) return false;
            if( !bool.TryParse( v, out var b ) ) Throw.CKException( $"Invalid \"{key}\": '{v}' (true or false is expected)." );
            return b;
        }
    }

    /// <summary>
    /// Gets the connection string of the default database ("db"). Null when not configured.
    /// </summary>
    public string? DefaultDatabaseConnectionString { get; }

    /// <summary>
    /// Gets the other databases.
    /// </summary>
    public IReadOnlyList<SqlDatabaseDescriptor> Databases { get; }

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
        // The 3 steps setup is an implementation detail of SQL Server: it has no configuration of its own.
        context.EngineConfiguration.EnsureAspect<SetupableAspectConfiguration>();
        var sql = context.EngineConfiguration.EnsureAspect<SqlSetupAspectConfiguration>();
        if( DefaultDatabaseConnectionString != null ) sql.DefaultDatabaseConnectionString = DefaultDatabaseConnectionString;
        foreach( var d in Databases )
        {
            sql.Databases.RemoveAll( x => x.LogicalDatabaseName == d.LogicalDatabaseName );
            sql.Databases.Add( d );
        }
        sql.GlobalResolution |= GlobalResolution;
        sql.IgnoreMissingDependencyIsError |= IgnoreMissingDependencyIsError;
        if( sql.DefaultDatabaseConnectionString == null )
        {
            monitor.Error( $"The \"SqlServer\" aspect has no \"DefaultDatabaseConnectionString\" (in the \"Aspects\" of the builder): {apps.Select( a => a.ToString() ).Concatenate()} need a database." );
            return false;
        }
        foreach( var a in apps )
        {
            a.ProjectValue( "SqlServer:DefaultDatabaseConnectionString", sql.DefaultDatabaseConnectionString );
        }
        return true;
    }
}
