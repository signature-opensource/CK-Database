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
        public static StObjEngineConfiguration BuildSetupConfig( 
            string connectionString,
            IEnumerable<string> assembliesToSetup,
            IEnumerable<string> recurseAssembliesToSetup,
            string dynamicAssemblyName, 
            string binPath,
            bool sourceGeneration )
        {
            var config = new StObjEngineConfiguration();
            config.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.AddRange( assembliesToSetup );
            config.BuildAndRegisterConfiguration.Assemblies.DiscoverRecurseAssemblyNames.AddRange( recurseAssembliesToSetup );
            config.FinalAssemblyConfiguration.AssemblyName = dynamicAssemblyName;
            config.FinalAssemblyConfiguration.Directory = binPath;
            config.FinalAssemblyConfiguration.SourceGeneration = sourceGeneration;
            var setupable = new SetupableAspectConfiguration();
            config.Aspects.Add( setupable );
            var sql = new SqlSetupAspectConfiguration
            {
                DefaultDatabaseConnectionString = connectionString,
                IgnoreMissingDependencyIsError = true // Set to true while we don't have SqlFragment support.
            };
            config.Aspects.Add( sql );

            return config;
        }

    }
}
