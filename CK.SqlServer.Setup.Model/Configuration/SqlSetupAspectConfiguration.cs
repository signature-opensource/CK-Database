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

namespace CK.Setup
{
    [Serializable]
    public class SqlSetupAspectConfiguration : ISetupEngineAspectConfiguration
    {
        readonly List<SqlDatabaseDescriptor> _databases;
        readonly List<string> _ckPackageDirectories;
        readonly List<string> _sqlFileDirectories;
        string _defaultDatabaseConnectionString;
        bool _ignoreMissingDependencyIsError;

        /// <summary>
        /// Initializes a new <see cref="SqlSetupAspectConfiguration"/>.
        /// </summary>
        public SqlSetupAspectConfiguration()
        {
            _databases = new List<SqlDatabaseDescriptor>();
            _ckPackageDirectories = new List<string>();
            _sqlFileDirectories = new List<string>();
        }

        /// <summary>
        /// Gets or sets the default database connection string.
        /// </summary>
        public string DefaultDatabaseConnectionString
        {
            get { return _defaultDatabaseConnectionString; }
            set { _defaultDatabaseConnectionString = value; }
        }

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
            foreach( var desc in Databases ) if( desc.DatabaseName == name ) return desc.ConnectionString;
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
        /// Defaults to false: objects that support a MissingDependencyIsError property (sets to true) will fail with an error if a missing dependency is detected.
        /// This applies to all <see cref="Databases"/>.
        /// </summary>
        public bool IgnoreMissingDependencyIsError
        {
            get { return _ignoreMissingDependencyIsError; }
            set { _ignoreMissingDependencyIsError = value; }
        }

        /// <summary>
        /// Gets the list of root directories (lookup is recursive) into which file packages (*.ck xml files) must be registered.
        /// </summary>
        public List<string> FilePackageDirectories => _ckPackageDirectories; 

        /// <summary>
        /// Gets the list of root directories (lookup is recursive) into which sql files (*.sql files) must be registered.
        /// </summary>
        public List<string> SqlFileDirectories =>_sqlFileDirectories; 
    }
}
