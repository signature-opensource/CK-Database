using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;

namespace CKDBSetup
{
    static class DbSetupHelper
    {
        public static SetupEngineConfiguration BuildSetupConfig( 
            string connectionString,
            IEnumerable<string> assembliesToSetup,
            IEnumerable<string> recurseAssembliesToSetup,
            string dynamicAssemblyName, 
            string binPath,
            SetupEngineRunningMode runningMode,
            bool sourceGeneration )
        {
            var config = new SetupEngineConfiguration();
            config.RunningMode = runningMode;
            config.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.AddRange( assembliesToSetup );
            config.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverRecurseAssemblyNames.AddRange( recurseAssembliesToSetup );
            config.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = dynamicAssemblyName;
            config.StObjEngineConfiguration.FinalAssemblyConfiguration.Directory = binPath;
            config.StObjEngineConfiguration.FinalAssemblyConfiguration.SourceGeneration = sourceGeneration;
            var c = new SqlSetupAspectConfiguration
            {
                DefaultDatabaseConnectionString = connectionString,
                IgnoreMissingDependencyIsError = true // Set to true while we don't have SqlFragment support.
            };
            config.Aspects.Add( c );

            return config;
        }

    }
}
