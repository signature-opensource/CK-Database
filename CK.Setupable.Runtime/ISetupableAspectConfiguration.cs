using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Setup phasis configuration interface.
    /// Can be injected into other aspect by using a <see cref="ConfigureOnly{T}"/> parameter.
    /// </summary>
    public interface ISetupableAspectConfiguration
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
        /// (an object can be both)and/or <see cref="IEnumerable"/> of such objects (recursively) that
        /// must participate to Setup.
        /// </summary>
        IList<object> ExternalItems { get; }

        /// <summary>
        /// Gets or sets a function that will be called with the list of items once all of them are registered.
        /// This can be used to analyse information about items registration and ordering.
        /// </summary>
        Action<IEnumerable<IDependentItem>> DependencySorterHookInput { get; set; }

        /// <summary>
        /// Gets or sets a function that will be called when items have been sorted.
        /// The final <see cref="IDependencySorterResult"/> may not be successful (ie. <see cref="IDependencySorterResult.HasStructureError"/> may be true),
        /// but if a cycle has been detected, this hook is not called.
        /// This can be used to analyse information about items registration and ordering.
        /// </summary>
        Action<IEnumerable<ISortedItem>> DependencySorterHookOutput { get; set; }

    }
}
