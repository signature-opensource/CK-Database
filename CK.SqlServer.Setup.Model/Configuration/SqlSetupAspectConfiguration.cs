#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\Configuration\SqlSetupAspectConfiguration.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Setup;
using System.Xml.Linq;
using System.Linq;

namespace CK.Setup
{
    [Serializable]
    public class SqlSetupAspectConfiguration : ISetupEngineAspectConfiguration
    {
        readonly List<SqlDatabaseDescriptor> _databases;

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspectConfiguration"/>.
        /// </summary>
        public SqlSetupAspectConfiguration()
        {
            _databases = new List<SqlDatabaseDescriptor>();
        }

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspectConfiguration"/> from its xml representation.
        /// </summary>
        /// <param name="e">The element.</param>
        public SqlSetupAspectConfiguration( XElement e )
        {
            XName xDatabases = XNamespace.None + "Databases";
            XName xDefaultDatabaseConnectionString = XNamespace.None + "DefaultDatabaseConnectionString";
            XName xGlobalResolution = XNamespace.None + "GlobalResolution";
            XName xIgnoreMissingDependencyIsError = XNamespace.None + "IgnoreMissingDependencyIsError";

            _databases = e.Elements( xDatabases ).Select( d => new SqlDatabaseDescriptor( d ) ).ToList();
            DefaultDatabaseConnectionString = e.Element( xDefaultDatabaseConnectionString )?.Value;
            GlobalResolution = string.Equals( e.Element( xGlobalResolution )?.Value, "true", StringComparison.OrdinalIgnoreCase );
            IgnoreMissingDependencyIsError = string.Equals( e.Element( xIgnoreMissingDependencyIsError )?.Value, "true", StringComparison.OrdinalIgnoreCase );
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
            if( name == SqlDatabase.DefaultDatabaseName ) return DefaultDatabaseConnectionString;
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
