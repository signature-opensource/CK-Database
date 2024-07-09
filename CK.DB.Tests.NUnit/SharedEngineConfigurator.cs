using CK.Testing;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    static class SharedEngineConfigurator
    {
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
        internal static void AutoConfigure()
        {
            SharedEngine.AutoConfigure += engineConfiguration =>
            {
                engineConfiguration.EnsureSqlServerConfigurationAspect();
            };
        }
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    }
}
