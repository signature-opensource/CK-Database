using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CK.Setup
{
    public class SqlSetupAspectConfiguration : ISetupEngineAspectConfiguration
    {
        /// <summary>
        /// Default database name is "db".
        /// </summary>
        public const string DefaultDatabaseName = "db";

        readonly List<SqlDatabaseDescriptor> _databases;

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspectConfiguration"/>.
        /// </summary>
        public SqlSetupAspectConfiguration()
        {
            _databases = new List<SqlDatabaseDescriptor>();
        }

        static readonly XName xDatabases = XNamespace.None + "Databases";
        static readonly XName xDatabase = XNamespace.None + "Database";
        static readonly XName xDefaultDatabaseConnectionString = XNamespace.None + "DefaultDatabaseConnectionString";
        static readonly XName xGlobalResolution = XNamespace.None + "GlobalResolution";
        static readonly XName xIgnoreMissingDependencyIsError = XNamespace.None + "IgnoreMissingDependencyIsError";

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspectConfiguration"/> from its xml representation.
        /// </summary>
        /// <param name="e">The element.</param>
        public SqlSetupAspectConfiguration( XElement e )
        {
            _databases = e.Elements( xDatabases ).Elements( xDatabase ).Select( d => new SqlDatabaseDescriptor( d ) ).ToList();
            DefaultDatabaseConnectionString = e.Element( xDefaultDatabaseConnectionString )?.Value;
            GlobalResolution = string.Equals( e.Element( xGlobalResolution )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            IgnoreMissingDependencyIsError = string.Equals( e.Element( xIgnoreMissingDependencyIsError )?.Value, "true", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The <see cref="SqlSetupAspectConfiguration(XElement)"/> constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( new XElement( xDatabases, _databases.Select( d => d.Serialize( new XElement( xDatabase ) ) ) ),
                   new XElement( xDefaultDatabaseConnectionString, DefaultDatabaseConnectionString ),
                   GlobalResolution ? new XElement( xGlobalResolution, "true" ) : null,
                   IgnoreMissingDependencyIsError ? new XElement( xGlobalResolution, "true" ) : null );
            return e;
        }
        /// <summary>
        /// Gets or sets the default database connection string.
        /// </summary>
        public string DefaultDatabaseConnectionString { get; set; }

        /// <summary>
        /// Gets the list of available <see cref="SqlDatabaseDescriptor"/>.
        /// </summary>
        public List<SqlDatabaseDescriptor> Databases => _databases; 

        /// <summary>
        /// Finds a configured connection string by its name.
        /// It may be the <see cref="DefaultDatabaseConnectionString"/> (default database name is 'db') or one of the registered <see cref="Databases"/>.
        /// </summary>
        /// <param name="name">Logical name of the connection string to find.</param>
        /// <returns>Configured connection string or null if not found.</returns>
        public string FindConnectionStringByName( string name )
        {
            if( name == DefaultDatabaseName ) return DefaultDatabaseConnectionString;
            foreach( var desc in Databases ) if( desc.LogicalDatabaseName == name ) return desc.ConnectionString;
            return null;
        }

        /// <summary>
        /// Gets or set whether the resolution of objects must be done globally.
        /// This is a temporary property: this should eventually be the only mode...
        /// </summary>
        public bool GlobalResolution { get; set; }

        string ISetupEngineAspectConfiguration.AspectType => "CK.SqlServer.Setup.SqlSetupAspect, CK.SqlServer.Setup.Engine"; 

        
        /// <summary>
        /// Gets or sets whether when installing, the informational message 'The module 'X' depends 
        /// on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must always be logged as a <see cref="LogLevel.Info"/>.
        /// Defaults to false.
        /// This applies to all <see cref="Databases"/>.
        /// </summary>
        public bool IgnoreMissingDependencyIsError { get; set; }

    }
}
