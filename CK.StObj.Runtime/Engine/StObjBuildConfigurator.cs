using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Template class that implements a Chain of Responsibility pattern on the different hooks called
    /// during the StObj build phasis (except the <see cref="IStObjRuntimeBuilder"/> methods).
    /// These configurator must be added to a <see cref="StObjEngineConfigurator"/>.
    /// It does nothing at its level except calling the <see cref="Next"/> configurator if it is not null.
    /// Methods are defined here in the order where they are called.
    /// </summary>
    public class StObjBuildConfigurator : IAmbientContractDispatcher, IStObjStructuralConfigurator, IStObjValueResolver
    {
        StObjBuildConfigurator _next;
        StObjEngineConfigurator _host;

        /// <summary>
        /// Gets the next <see cref="StObjBuildConfigurator"/> that should be called by all hooks in this configurator.
        /// Can be null.
        /// </summary>
        public StObjBuildConfigurator Next
        {
            get { return _next; }
            internal set { _next = value; }
        }

        /// <summary>
        /// Gets the configuration host to which this configurator has been added.
        /// Null if this configurator is not bound to a <see cref="StObjEngineConfigurator"/>.
        /// </summary>
        public StObjEngineConfigurator Host
        {
            get { return _host; }
            internal set { _host = value; }
        }

        /// <summary>
        /// Step n°1 - Called during Assembly/Types discovering: allows a Type not marked with <see cref="IAmbientContract"/> to be considered as an Ambiant Contract.
        /// Empty implementation of <see cref="IAmbientContractDispatcher.IsAmbientContractClass"/>.
        /// Returns the result of the <see cref="Next"/> if it exist, otherwise returns always false: only classes that are explicitely marked with <see cref="IAmbientContract"/>
        /// or types that inherit from a <see cref="IAmbientContractDefiner"/> are considered as Ambient Contracts.
        /// </summary>
        /// <param name="t">A type that is not, structurally through the interfaces it supports, an Ambient Contract.</param>
        /// <returns>True to consider the given type (and all its specializations) as an Ambient Contract.</returns>
        public virtual bool IsAmbientContractClass( Type t )
        {
            return _next != null ? _next.IsAmbientContractClass( t ) : false;
        }

        /// <summary>
        /// Step n°2 - Once Ambient Contracts have been discovered, this allows types to be removed/added to different contexts.
        /// This empty implementation of <see cref="IAmbientContractDispatcher.Dispatch"/> calls <see cref="Next"/> if it is not null.
        /// </summary>
        /// <param name="t">The type to map.</param>
        /// <param name="contexts">Context names into which the type is defined. This set can be changed.</param>
        public virtual void Dispatch( Type t, ISet<string> contexts )
        {
            if( _next != null ) _next.Dispatch( t, contexts );
        }

        /// <summary>
        /// Step n°3 - Once most specialized objects are created, the configuration for each "slice" (StObj) from top to bottom of the inheritance chain 
        /// can be altered: properties can be set, dependencies like Container, Requires, Children, etc. but also parameters' value of the StObjConstruct method can be changed.
        /// This empty implementation of <see cref="IStObjStructuralConfigurator.Configure"/> calls <see cref="Next"/> if it is not null.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="o">The item to configure.</param>
        public virtual void Configure( IActivityMonitor monitor, IStObjMutableItem o )
        {
            if( _next != null ) _next.Configure( monitor, o );
        }

        /// <summary>
        /// Step n°4 - Last step before ordering. Ambient properties that had not been resolved can be set to a value here.
        /// This empty implementation of <see cref="IStObjValueResolver.ResolveExternalPropertyValue"/> calls <see cref="Next"/> if it is not null.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="ambientProperty">Property for which a value should be set.</param>
        public virtual void ResolveExternalPropertyValue( IActivityMonitor monitor, IStObjFinalAmbientProperty ambientProperty )
        {
            if( _next != null ) _next.ResolveExternalPropertyValue( monitor, ambientProperty );
        }

        /// <summary>
        /// Step n°5 - StObj dependency graph has been ordered, properties that was settable before initialization
        /// have been set, the StObjConstruct method is called and for each of their parameters, this method enables
        /// the parameter value to be set or changed.
        /// This is the last step of the pure StObj level work: after this one, object graph dependencies have been resolved, objects are configured.
        /// This empty implementation of <see cref="IStObjValueResolver.ResolveParameterValue"/> calls <see cref="Next"/> if it is not null.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="parameter">Parameter of a StObjConstruct method.</param>
        public virtual void ResolveParameterValue( IActivityMonitor monitor, IStObjFinalParameter parameter )
        {
            if( _next != null ) _next.ResolveParameterValue( monitor, parameter );
        }
    }


}
