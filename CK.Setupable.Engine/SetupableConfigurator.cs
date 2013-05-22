using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Template class that concentrates the different hooks called during a setup phasis.
    /// </summary>
    public class SetupableConfigurator : IAmbientContractDispatcher, IStObjStructuralConfigurator, IStObjValueResolver, IStObjSetupConfigurator, IStObjSetupItemFactory, IStObjSetupDynamicInitializer, ISetupDriverFactory
    {
        SetupableConfigurator _previous;

        /// <summary>
        /// Empty configurator.
        /// </summary>
        public static readonly SetupableConfigurator Empty = new SetupableConfigurator( null );

        /// <summary>
        /// Initializes a new <see cref="SetupableConfigurator"/> bound to an optional previous one. 
        /// </summary>
        /// <param name="previous">Another configurator that should be called by all hooks in this configurator.</param>
        public SetupableConfigurator( SetupableConfigurator previous = null )
        {
            _previous = previous;
        }

        /// <summary>
        /// Gets or sets the previous <see cref="SetupableConfigurator"/> that will be called first by the different methods. Can be null.
        /// </summary>
        public SetupableConfigurator Previous 
        {
            get { return _previous; }
            set { _previous = value; }
        }

        /// <summary>
        /// Empty implementation of <see cref="IAmbientContractDispatcher.IsAmbientContractClass"/>.
        /// Returns the result of the <see cref="Previous"/> if it exist, oterwise returns always false: only classes that are explicitely marked with <see cref="IAmbientContract"/>
        /// or types that inherit from a <see cref="IAmbientContractDefiner"/> are considered as Ambient Contracts.
        /// </summary>
        /// <param name="t">A type that is not, structurally through the interfaces it supports, an Ambient Contract.</param>
        /// <returns>True to consider the given type (and all its specializations) as an Ambient Contract.</returns>
        public virtual bool IsAmbientContractClass( Type t )
        {
            return _previous != null ? _previous.IsAmbientContractClass( t ) : false;
        }

        /// <summary>
        /// Empty implementation of <see cref="IAmbientContractDispatcher.Dispatch"/> (calls <see cref="Previous"/> if it is not null).
        /// </summary>
        /// <param name="t">The type to map.</param>
        /// <param name="contexts">Context names into which the type is defined. This set can be changed.</param>
        public virtual void Dispatch( Type t, ISet<string> contexts )
        {
            if( _previous != null ) _previous.Dispatch( t, contexts );
        }

        /// <summary>
        /// Empty implementation of <see cref="IStObjStructuralConfigurator.Configure"/> (calls <see cref="Previous"/> if it is not null).
        /// </summary>
        /// <param name="o">The item to configure.</param>
        public virtual void Configure( IActivityLogger logger, IStObjMutableItem o )
        {
            if( _previous != null ) _previous.Configure( logger, o );
        }

        /// <summary>
        /// Empty implementation of <see cref="IStObjValueResolver.ResolveParameterValue"/> (calls <see cref="Previous"/> if it is not null).
        /// </summary>
        /// <param name="_logger">The _logger to use.</param>
        /// <param name="parameter">Parameter of a Construct method.</param>
        public virtual void ResolveParameterValue( IActivityLogger logger, IStObjFinalParameter parameter )
        {
            if( _previous != null ) _previous.ResolveParameterValue( logger, parameter );
        }

        /// <summary>
        /// Empty implementation of <see cref="IStObjValueResolver.ResolveExternalPropertyValue"/> (calls <see cref="Previous"/> if it is not null).
        /// </summary>
        /// <param name="_logger">The _logger to use.</param>
        /// <param name="ambientProperty">Property for which a value should be set.</param>
        public virtual void ResolveExternalPropertyValue( IActivityLogger logger, IStObjFinalAmbientProperty ambientProperty )
        {
            if( _previous != null ) _previous.ResolveExternalPropertyValue( logger, ambientProperty );
        }

        /// <summary>
        /// Empty implementation of <see cref="IStObjSetupConfigurator.ConfigureDependentItem"/> (calls <see cref="Previous"/> if it is not null).
        /// </summary>
        /// <param name="_logger">Logger to use.</param>
        /// <param name="data">Mutable data (typically initialized by attributes and other direct code-first approaches).</param>
        public virtual void ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
            if( _previous != null ) _previous.ConfigureDependentItem( logger, data );
        }

        /// <summary>
        /// Empty implementation of <see cref="IStObjSetupItemFactory.CreateDependentItem"/> (calls <see cref="Previous"/> if it is not null, otherwise returns null).
        /// </summary>
        /// <param name="_logger">Logger to use.</param>
        /// <param name="data">Descriptive data (initialized by attributes and other direct code-first approaches and configured by any <see cref="IStObjSetupConfigurator"/>).</param>
        /// <returns>A <see cref="IMutableSetupItem"/> implementation that must be correctly initialized by the given data, or null to use the default <see cref="StObjDynamicPackageItem"/>.</returns>
        public virtual IMutableSetupItem CreateDependentItem( IActivityLogger logger, IStObjSetupData data )
        {
            return _previous != null ? _previous.CreateDependentItem( logger, data ) : null;
        }

        /// <summary>
        /// Empty implementation of <see cref="IStObjSetupDynamicInitializer.DynamicItemInitialize"/> (calls <see cref="Previous"/> if it is not null).
        /// </summary>
        /// <param name="state">Context for dynamic initialization.</param>
        /// <param name="item">The setup item for the object slice.</param>
        /// <param name="stObj">The StObj (the object slice).</param>
        public virtual void DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjRuntime stObj )
        {
            if( _previous != null ) _previous.DynamicItemInitialize( state, item, stObj );
        }

        /// <summary>
        /// Creates a (potentially configured) instance of <see cref="SetupDriver"/> of a given <paramref name="driverType"/>.
        /// This empty implementation calls <see cref="Previous"/> if it is not null, otherwise it always returns null.
        /// </summary>
        /// <param name="driverType">SetupDriver type to create.</param>
        /// <param name="info">Internal constructor information.</param>
        /// <returns>A setup driver. Null if not able to create it (a basic <see cref="Activator.CreateInstance"/> will be used to create the driver).</returns>
        public virtual SetupDriver CreateDriver( Type driverType, SetupDriver.BuildInfo info )
        {
            return _previous != null ? _previous.CreateDriver( driverType, info ) : null;
        }

    }

}
