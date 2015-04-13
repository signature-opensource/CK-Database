#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\SetupableConfigurator.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Template class that concentrates the different hooks called during a setup phasis (except the <see cref="IStObjRuntimeBuilder"/> methods).
    /// Methods are defined here in the order where they are called.
    /// </summary>
    public class SetupEngineConfigurator : IAmbientContractDispatcher, IStObjStructuralConfigurator, IStObjValueResolver, IStObjSetupConfigurator, IStObjSetupItemFactory, IStObjSetupDynamicInitializer, ISetupDriverFactory
    {
        SetupEngineConfigurator _previous;

        /// <summary>
        /// Empty configurator.
        /// </summary>
        public static readonly SetupEngineConfigurator Empty = new SetupEngineConfigurator( null );

        /// <summary>
        /// Initializes a new <see cref="SetupEngineConfigurator"/> bound to an optional previous one. 
        /// </summary>
        /// <param name="previous">Another configurator that should be called by all hooks in this configurator.</param>
        public SetupEngineConfigurator( SetupEngineConfigurator previous = null )
        {
            _previous = previous;
        }

        /// <summary>
        /// Gets or sets the previous <see cref="SetupEngineConfigurator"/> that will be called first by the different methods. Can be null.
        /// </summary>
        public SetupEngineConfigurator Previous 
        {
            get { return _previous; }
            set { _previous = value; }
        }

        /// <summary>
        /// Step n°1 - Called during Assembly/Types discovering: allows a Type not marked with <see cref="IAmbientContract"/> to be considered as an Ambiant Contract.
        /// Empty implementation of <see cref="IAmbientContractDispatcher.IsAmbientContractClass"/>.
        /// Returns the result of the <see cref="Previous"/> if it exist, otherwise returns always false: only classes that are explicitely marked with <see cref="IAmbientContract"/>
        /// or types that inherit from a <see cref="IAmbientContractDefiner"/> are considered as Ambient Contracts.
        /// </summary>
        /// <param name="t">A type that is not, structurally through the interfaces it supports, an Ambient Contract.</param>
        /// <returns>True to consider the given type (and all its specializations) as an Ambient Contract.</returns>
        public virtual bool IsAmbientContractClass( Type t )
        {
            return _previous != null ? _previous.IsAmbientContractClass( t ) : false;
        }

        /// <summary>
        /// Step n°2 - Once Ambient Contracts have been discovered, this allows types to be removed/added to different contexts.
        /// This empty implementation of <see cref="IAmbientContractDispatcher.Dispatch"/> calls <see cref="Previous"/> if it is not null.
        /// </summary>
        /// <param name="t">The type to map.</param>
        /// <param name="contexts">Context names into which the type is defined. This set can be changed.</param>
        public virtual void Dispatch( Type t, ISet<string> contexts )
        {
            if( _previous != null ) _previous.Dispatch( t, contexts );
        }

        /// <summary>
        /// Step n°3 - Once most specialized objects are created, the configuration for each "slice" (StObj) from top to bottom of the inheritance chain 
        /// can be altered: properties can be set, dependencies like Container, Requires, Children, etc. but also parameters' value of the Construct method can be changed.
        /// This empty implementation of <see cref="IStObjStructuralConfigurator.Configure"/> calls <see cref="Previous"/> if it is not null.
        /// </summary>
        /// <param name="o">The item to configure.</param>
        public virtual void Configure( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( _previous != null ) _previous.Configure( monitor, o );
        }

        /// <summary>
        /// Step n°4 - Last step before ordering. Ambient properties that had not been resolved can be set to a value here.
        /// This empty implementation of <see cref="IStObjValueResolver.ResolveExternalPropertyValue"/> calls <see cref="Previous"/> if it is not null.
        /// </summary>
        /// <param name="_monitor">The _monitor to use.</param>
        /// <param name="ambientProperty">Property for which a value should be set.</param>
        public virtual void ResolveExternalPropertyValue( IActivityMonitor monitor, IStObjFinalAmbientProperty ambientProperty )
        {
            if( _previous != null ) _previous.ResolveExternalPropertyValue( monitor, ambientProperty );
        }

        /// <summary>
        /// Step n°5 - StObj dependency graph has been ordered, properties that was settable before initialization have been set, the Construct method
        /// is called and for each of their parameters, this enable the parameter value to be set or changed.
        /// This is the last step of the pure StObj level work: after this one, object graph dependencies have been resolved, objects are configured.
        /// This empty implementation of <see cref="IStObjValueResolver.ResolveParameterValue"/> (calls <see cref="Previous"/> if it is not null.
        /// </summary>
        /// <param name="_monitor">The _monitor to use.</param>
        /// <param name="parameter">Parameter of a Construct method.</param>
        public virtual void ResolveParameterValue( IActivityMonitor monitor, IStObjFinalParameter parameter )
        {
            if( _previous != null ) _previous.ResolveParameterValue( monitor, parameter );
        }

        /// <summary>
        /// Step n°6 - Entering the Setupable level: StObjSetupData are created for each StObj and this method allows to configure their setup item and 
        /// driver type to use, versions, requirements and other properties related to the three-steps setup phasis.
        /// This empty implementation of <see cref="IStObjSetupConfigurator.ConfigureDependentItem"/> calls <see cref="Previous"/> if it is not null.
        /// </summary>
        /// <param name="_monitor">Monitor to use.</param>
        /// <param name="data">Mutable data (typically initialized by attributes and other direct code-first approaches).</param>
        public virtual void ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data )
        {
            if( _previous != null ) _previous.ConfigureDependentItem( monitor, data );
        }

        /// <summary>
        /// Step n°7 - Creation of the actual SetupItem to use for a StObj may be decided here. Like the others, this step is optional: by default
        /// a generic <see cref="StObjDynamicPackageItem"/> does the job.
        /// This empty implementation of <see cref="IStObjSetupItemFactory.CreateDependentItem"/> calls <see cref="Previous"/> if it is not null, otherwise returns null.
        /// </summary>
        /// <param name="_monitor">Monitor to use.</param>
        /// <param name="data">Descriptive data (initialized by attributes and other direct code-first approaches and configured by any <see cref="IStObjSetupConfigurator"/>).</param>
        /// <returns>A <see cref="IMutableSetupItem"/> implementation that must be correctly initialized by the given data, or null to use the default <see cref="StObjDynamicPackageItem"/>.</returns>
        public virtual IMutableSetupItem CreateDependentItem( IActivityMonitor monitor, IStObjSetupData data )
        {
            return _previous != null ? _previous.CreateDependentItem( monitor, data ) : null;
        }

        /// <summary>
        /// Step n°8 - This is where new <see cref="IDependentItem"/>s can be created and registered (typically as children of the item). For Sql, this is the step
        /// where setup items of stored procedures are instanciated and attached to their declaring tables or package.
        /// This empty implementation of <see cref="IStObjSetupDynamicInitializer.DynamicItemInitialize"/> calls <see cref="Previous"/> if it is not null.
        /// </summary>
        /// <param name="state">Context for dynamic initialization.</param>
        /// <param name="item">The setup item for the object slice.</param>
        /// <param name="stObj">The StObj (the object slice).</param>
        public virtual void DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            if( _previous != null ) _previous.DynamicItemInitialize( state, item, stObj );
        }

        /// <summary>
        /// Step n°9 - This is the last step that is not called if <see cref="SetupEngineConfiguration.RunningMode"/> is <see cref="SetupEngineRunningMode.StObjLayerOnly"/>: 
        /// the dependency graph of the setup items (StObj and/or pure <see cref="IDependentItem"/>) has been resolved, we now create the Setup Drivers for each of them that 
        /// will support the three-steps setup phasis.
        /// Creates a (potentially configured) instance of <see cref="GenericItemSetupDriver"/> of a given <paramref name="driverType"/>.
        /// This empty implementation calls <see cref="Previous"/> if it is not null, otherwise it always returns null.
        /// </summary>
        /// <param name="driverType">SetupDriver type to create.</param>
        /// <param name="info">Internal constructor information.</param>
        /// <returns>A setup driver. Null if not able to create it (a basic <see cref="Activator.CreateInstance(Type)"/> will be used to create the driver).</returns>
        public virtual GenericItemSetupDriver CreateDriver( Type driverType, GenericItemSetupDriver.BuildInfo info )
        {
            return _previous != null ? _previous.CreateDriver( driverType, info ) : null;
        }

    }

}
