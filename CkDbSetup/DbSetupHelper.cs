using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;

namespace CkDbSetup
{
    static class DbSetupHelper
    {
        public static SetupEngineConfiguration BuildSetupConfig( string connectionString, IEnumerable<string> assembliesToSetup, string dynamicAssemblyName, string binPath )
        {
            var config = new SetupEngineConfiguration();

            foreach( var a in assembliesToSetup )
            {
                config.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.Add( a );
            }

            config.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = dynamicAssemblyName;
            config.StObjEngineConfiguration.FinalAssemblyConfiguration.Directory = binPath;

            var c = new SqlSetupAspectConfiguration
            {
                DefaultDatabaseConnectionString = connectionString,
                IgnoreMissingDependencyIsError = true // Set to true while we don't have SqlFragment support.
            };

            config.Aspects.Add( c );

            return config;
        }
        public static bool ExecuteDbSetup( IActivityMonitor m, SetupEngineConfiguration config )
        {
            using( var r = StObjContextRoot.Build( config, null, m, true ) )
            {
                return r.Success;
            }
        }
    }
}
