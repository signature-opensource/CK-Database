using CK.Core;
using System;

namespace CK.Setup
{
    /// <summary>
    /// Template class that implements a Chain of Responsibility pattern on the different hooks called
    /// during the setup phasis.
    /// These configurator must be added to a <see cref="SetupAspectConfigurator"/>.
    /// It does nothing at its level except calling the <see cref="Next"/> configurator if it is not null.
    /// Methods are defined here in the order where they are called.
    /// </summary>
    public class SetupConfigurationLayer : IStObjSetupConfigurator, IStObjSetupItemFactory, IStObjSetupDynamicInitializer, ISetupDriverFactory
    {
        SetupConfigurationLayer _next;
        SetupAspectConfigurator _host;

        /// <summary>
        /// Gets the next <see cref="SetupConfigurationLayer"/> that should be called by all hooks in this configurator.
        /// Can be null.
        /// </summary>
        public SetupConfigurationLayer Next
        {
            get { return _next; }
            internal set { _next = value; }
        }

        /// <summary>
        /// Gets the configuration host to which this configurator has been added.
        /// Null if this configurator is not bound to a <see cref="SetupAspectConfigurator"/>.
        /// </summary>
        public SetupAspectConfigurator Host
        {
            get { return _host; }
            internal set { _host = value; }
        }

        /// <summary>
        /// Step n°1 - Entering the Setupable level: StObjSetupData are created for each StObj and this method allows to configure their setup item and 
        /// driver type to use, versions, requirements and other properties related to the three-steps setup phasis.
        /// This empty implementation of <see cref="IStObjSetupConfigurator.ConfigureDependentItem"/> calls <see cref="Next"/> if it is not null.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Mutable data (typically initialized by attributes and other direct code-first approaches).</param>
        public virtual void ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data )
        {
            if( _next != null ) _next.ConfigureDependentItem( monitor, data );
        }

        /// <summary>
        /// Step n°2 - Creation of the actual SetupItem to use for a StObj may be decided here. Like the others, this step is optional: by default
        /// a generic <see cref="StObjDynamicPackageItem"/> does the job.
        /// This empty implementation of <see cref="IStObjSetupItemFactory.CreateSetupItem"/> calls <see cref="Next"/> if it is not null, otherwise returns null.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Descriptive data (initialized by attributes and other direct code-first approaches and configured by any <see cref="IStObjSetupConfigurator"/>).</param>
        /// <returns>A <see cref="IStObjSetupItem"/> implementation that must be correctly initialized by the given data, or null to use the default <see cref="StObjDynamicPackageItem"/>.</returns>
        public virtual IStObjSetupItem CreateSetupItem( IActivityMonitor monitor, IStObjSetupData data )
        {
            return _next != null ? _next.CreateSetupItem( monitor, data ) : null;
        }

        /// <summary>
        /// Step n°3 - This is where new <see cref="IDependentItem"/>s can be created and registered (typically as children of the item). For Sql, this is the step
        /// where setup items of stored procedures are instanciated and attached to their declaring tables or package.
        /// This empty implementation of <see cref="IStObjSetupDynamicInitializer.DynamicItemInitialize"/> calls <see cref="Next"/> if it is not null.
        /// </summary>
        /// <param name="state">Context for dynamic initialization.</param>
        /// <param name="item">The setup item for the object slice.</param>
        /// <param name="stObj">The StObj (the object slice).</param>
        public virtual void DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            if( _next != null ) _next.DynamicItemInitialize( state, item, stObj );
        }

        /// <summary>
        /// Step n°4 - The dependency graph of the setup items (StObj and/or pure <see cref="IDependentItem"/>) has been resolved, we now create the Setup Drivers for each of them that 
        /// will support the three-steps setup phasis.
        /// Creates a (potentially configured) instance of <see cref="SetupItemDriver"/> of a given <paramref name="driverType"/>.
        /// This empty implementation calls <see cref="Next"/> if it is not null, otherwise it always returns null.
        /// </summary>
        /// <param name="driverType">SetupDriver type to create.</param>
        /// <param name="info">Internal constructor information.</param>
        /// <returns>A setup driver. Null if not able to create it (<see cref="ServiceProviderExtension.SimpleObjectCreate(IServiceProvider, IActivityMonitor, Type, object)"/> will be used to create the driver).</returns>
        public virtual SetupItemDriver CreateDriver( Type driverType, SetupItemDriver.BuildInfo info )
        {
            return _next != null ? _next.CreateDriver( driverType, info ) : null;
        }

    }


}
