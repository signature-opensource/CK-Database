using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    [Serializable]
    public class SqlSetupCenterConfiguration : IStObjEngineConfiguration
    {
        readonly SetupCenterConfiguration _config;
        readonly List<SqlDatabaseDescriptor> _databases;
        readonly List<string> _ckPackageDirectories;
        readonly List<string> _sqlFileDirectories;
        string _defaultDatabaseConnectionString;
        bool _ignoreMissingDependencyIsError;

        /// <summary>
        /// Initializes a new <see cref="SqlSetupCenterConfiguration"/> with a new, uninitialized <see cref="SetupConfiguration"/>.
        /// </summary>
        public SqlSetupCenterConfiguration()
        {
            _config = new SetupCenterConfiguration();
            _config.ExplicitRegisteredClasses.Add( typeof( SqlDefaultDatabase ) );
            _databases = new List<SqlDatabaseDescriptor>();
            _ckPackageDirectories = new List<string>();
            _sqlFileDirectories = new List<string>();
            _config.AppDomainConfiguration.UseIndependentAppDomain = true;
        }

        /// <summary>
        /// Configuration for <see cref="SetupCenter"/>.
        /// Note that, by default, <see cref="SetupCenterConfiguration.AppDomainConfiguration"/>.<see cref="BuilderAppDomainConfiguration.UseIndependentAppDomain">UseIndependentAppDomain</see>
        /// is set to true.
        /// </summary>
        public SetupCenterConfiguration SetupConfiguration
        {
            get { return _config; }
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
        /// Gets the list of available <see cref="SqlDatabaseDescriptor"/>.
        /// </summary>
        public List<SqlDatabaseDescriptor> Databases
        {
            get { return _databases; }
        }

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
        /// Gets the list of root directories (lookup is recursive) into which file packages (*.ck xml files) must be registered.
        /// </summary>
        public List<string> FilePackageDirectories
        {
            get { return _ckPackageDirectories; }
        }

        /// <summary>
        /// Gets the list of root directories (lookup is recursive) into which sql files (*.sql files) must be registered.
        /// </summary>
        public List<string> SqlFileDirectories
        {
            get { return _sqlFileDirectories; }
        }


        #region IStObjEngineConfiguration members

        string IStObjEngineConfiguration.BuilderAssemblyQualifiedName
        {
            get { return "CK.SqlServer.Setup.SqlSetupCenter, CK.SqlServer.Setup.Engine"; }
        }

        BuilderFinalAssemblyConfiguration IStObjEngineConfiguration.FinalAssemblyConfiguration
        {
            get { return _config.FinalAssemblyConfiguration ; }
        }

        BuilderAppDomainConfiguration IStObjEngineConfiguration.AppDomainConfiguration
        {
            get { return _config.AppDomainConfiguration; }
        }

        #endregion
    }
}
