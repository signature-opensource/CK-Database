using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// Setup phasis configuration interface.
    /// Can be injected into other aspect by using a <see cref="ConfigureOnly{T}"/> parameter.
    /// </summary>
    public interface ISetupableAspectRunConfiguration
    {
        /// <summary>
        /// Gets the external aspect configuration object.
        /// </summary>
        SetupableAspectConfiguration ExternalConfiguration { get; }

        /// <summary>
        /// Gets the root of the <see cref="SetupConfigurationLayer"/> chain of responsibility.
        /// Aspects can add any needed configuration layer to it.
        /// </summary>
        SetupAspectConfigurator Configurator { get; }

        /// <summary>
        /// Provides a way to register any number of <see cref="IDependentItem"/>, <see cref="IDependentItemDiscoverer"/>
        /// (an object can be both)and/or IEnumerable of such objects (recursively) that
        /// must participate to Setup.
        /// </summary>
        IList<object> ExternalItems { get; }


    }
}
