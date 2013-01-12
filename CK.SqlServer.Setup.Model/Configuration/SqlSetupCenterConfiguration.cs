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
            UseIndependantAppDomain = true;
        }

        /// <summary>
        /// Configuration for <see cref="SetupCenter"/>.
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
        /// Gets the list of available <see cref="SqlDatabaseDescriptor"/>.
        /// </summary>
        public List<SqlDatabaseDescriptor> Databases
        {
            get { return _databases; }
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

        /// <summary>
        /// Gets or sets whether the setup phasis must be executed in a new AppDomain.
        /// Defaults to true.
        /// </summary>
        public bool UseIndependantAppDomain { get; set; }


        #region IStObjEngineConfiguration members

        string IStObjEngineConfiguration.BuilderAssemblyQualifiedName
        {
            get { return "CK.SqlServer.Setup.SqlSetupCenter, CK.SqlServer.Setup.Engine"; }
        }

        StObjFinalAssemblyConfiguration IStObjEngineConfiguration.StObjFinalAssemblyConfiguration
        {
            get { return _config.StObjFinalAssemblyConfiguration; }
        }

        #endregion

    }
}
