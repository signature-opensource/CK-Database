using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Database objects hold the <see cref="ConnectionString"/> and the schemes defined in it.
    /// </summary>
    [CKTypeDefiner]
    [Setup( ItemKind = DependentItemKindSpec.Group,
            TrackAmbientProperties = TrackAmbientPropertiesMode.AddPropertyHolderAsChildren,
            ItemTypeName = "CK.SqlServer.Setup.SqlDatabaseItem,CK.SqlServer.Setup.Runtime" )]
    public class SqlDatabase : SqlServer.ISqlConnectionStringProvider, IRealObject
    {
        /// <summary>
        /// Default database name is "db": this is the name of the <see cref="SqlDefaultDatabase"/> type.
        /// </summary>
        public const string DefaultDatabaseName = Setup.SqlSetupAspectConfiguration.DefaultDatabaseName;

        /// <summary>
        /// Default schema name is "CK": <see cref="SqlDefaultDatabase"/> registers it.
        /// </summary>
        public const string DefaultSchemaName = "CK";

        readonly string _name;
        readonly Dictionary<string,string> _schemas;
        bool _hasCKCore;
        bool _useSnapshotIsolation;

        /// <summary>
        /// Initializes a new <see cref="SqlDatabase"/>.
        /// </summary>
        /// <param name="name">Logical name of the database.</param>
        public SqlDatabase( string name )
        {
            if( String.IsNullOrWhiteSpace( name ) ) throw new ArgumentException( "Must be not null, empty, nor whitespace.", "name" );
            _name = name;
            _schemas = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
            ConnectionString = String.Empty;
        }

        /// <summary>
        /// Gets the logical name of the database. 
        /// This name, which is strongly associated to this SqlDatabase object and can not be changed (set only in the constructor), 
        /// defines the location of objects that are bound to it and drives the actual connection string to use.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets or sets the connection string.
        /// This can be automatically configured during setup (if the specialized class implements a StObjConstruct method with a connectionString parameter
        /// and sets this property).
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Finds or creates the given schema. 
        /// Schema names are case sensitive and this constraint is enforced here: an exception will 
        /// be thrown whenever casing differ between schema registration.
        /// </summary>
        /// <param name="name">Name of the schema.</param>
        /// <returns>Registered name.</returns>
        public string EnsureSchema( string name )
        {
            if( string.IsNullOrWhiteSpace( name ) ) throw new ArgumentException( "Must be not null, empty, nor whitespace.", "name" );
            if( _schemas.TryGetValue( name, out var existing ) )
            {
                if( name != existing )
                {
                    throw new CKException( "Casing must be strictly the same. '{0}' differs from '{1}'.", name, existing );
                }
            }
            else _schemas.Add( name, (existing = name) );
            return existing;
        }

        /// <summary>
        /// Gets the different schemes that are owned by this <see cref="SqlDatabase"/>.
        /// </summary>
        public IEnumerable<string> Schemas => _schemas.Keys; 

        /// <summary>
        /// Default database name is <see cref="DefaultDatabaseName"/> = "db".
        /// </summary>
        public bool IsDefaultDatabase => _name == DefaultDatabaseName;

        /// <summary>
        /// Gets whether CKCore schema with its helpers is installed in the database.
        /// <para>
        /// This can only be set by the configuration (handled by engine's SqlSetupAspect) of the <see cref="SqlSetupAspectConfiguration.Databases"/>.
        /// When false, no guaranty exists: this totally depends on the database, no attempt is made to alter it in any way.
        /// <para>
        /// Defaults to false (since v19 - the first net6 version and 18.1.0 in NetCore3): in previous versions all databases were initialized with snapshot isolation.
        /// </para>
        /// This is always true for the default database and not configurable (when <see cref="SqlDatabase.IsDefaultDatabase"/> is true).
        /// </para>
        /// </summary>
        public bool HasCKCore  => _hasCKCore | IsDefaultDatabase;

        /// <summary>
        /// Gets whether snapshot isolation ("SET ALLOW_SNAPSHOT_ISOLATION ON") is configured on the database
        /// and activated ("SET READ_COMMITTED_SNAPSHOT ON") so that the default READ_COMITTED is actually READ_COMMITTED_SNAPSHOT.
        /// <para>
        /// This can only be set by the configuration (handled by engine's SqlSetupAspect) of the <see cref="SqlSetupAspectConfiguration.Databases"/>.
        /// When false, no guaranty exists: this totally depends on the database, no attempt is made to alter it in any way.
        /// <para>
        /// Defaults to false (since v19 - the first net6 version and 18.1.0 in NetCore3): in previous versions all databases were initialized with snapshot isolation.
        /// </para>
        /// This is always true for the default database and not configurable (<see cref="SqlDatabase.IsDefaultDatabase"/> is true).
        /// </para>
        /// <para>
        /// See https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/snapshot-isolation-in-sql-server)
        /// </para>
        /// </summary>
        public bool UseSnapshotIsolation => _useSnapshotIsolation | IsDefaultDatabase;

        /// <summary>
        /// The parameters are injected by the engine's SqlSetupAspect.StObjConfiguratorHook.
        /// Do not change these parameter names nor the default values!
        /// </summary>
        void StObjConstruct( string? connectionString = null, bool hasCKCore = false, bool useSnapshotIsolation = false )
        {
            ConnectionString = connectionString ?? string.Empty;
            _hasCKCore = hasCKCore;
            _useSnapshotIsolation = useSnapshotIsolation;
        }

    }
}
