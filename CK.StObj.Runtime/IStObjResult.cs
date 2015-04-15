#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Runtime\IStObjResult.cs) is part of CK-Database. 
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
    /// A StObj "slices" a Structured Object (that is an <see cref="IAmbientContract"/>) by types in its inheritance chain.
    /// The <see cref="InitialObject">Structured Object</see> itself is built based on already built dependencies from top to bottom thanks to its "Construct" methods. 
    /// This interface is available after the dependency graph ordering (this is the Owner exposed by <see cref="IStObjFinalParameter"/> for construct parameters for instance).
    /// It is the final interface that is exposed for each StObj at the end of the StObjCollector.GetResults work.
    /// </summary>
    public interface IStObjResult : IStObj
    {
        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object).
        /// This instance is built at the beginning of the process and remains the same: it is not necessarily a "real" object since its auto-implemented methods
        /// are not generated (only stupid default stub implementation are created to be able to instantiate it).
        /// Once the dynamic assembly has been generated (and if StObjCollector.InjectFinalObjectAccessor has been called), the <see cref="ObjectAccessor"/>
        /// is updated to obtain a "real" object from the <see cref="StObjContextRoot"/>.
        /// </summary>
        object InitialObject { get; }

        /// <summary>
        /// Gets a function that returns the associated object instance (the final, most specialized, structured object).
        /// The <see cref="InitialObject"/> instance is built at the beginning of the process and remains the same until the dynamic assembly has been 
        /// generated and StObjCollector.InjectFinalObjectAccessor has been called: at this point, the object obtained by this accessor will be a "real" object
        /// from the dynamic assembly with all its auto-implemented methods available.
        /// <para>
        /// Once the final assembly has been generated, this function is updated with <see cref="IContextualStObjMap.Obtain"/>: during the setup phasis, the actual 
        /// objects that are associated to items are "real" objects produced/managed by the final <see cref="StObjContextRoot"/>.
        /// </para>
        /// <para>
        /// In order to honor potential transient lifetime (one day), these object should not be aggressively cached, this is why this is a function 
        /// and not a simple 'Object' or 'FinalObject' property. 
        /// </para>
        /// </summary>
        Func<object> ObjectAccessor { get; }

        /// <summary>
        /// Gets the provider for attributes. Attributes that are marked with <see cref="IAttributeAmbientContextBound"/> are cached
        /// and can keep an internal state if needed.
        /// </summary>
        /// <remarks>
        /// All attributes related to <see cref="ObjectType"/> (either on the type itself or on any of its members) should be retrieved 
        /// thanks to this method otherwise stateful attributes will not work correctly.
        /// </remarks>
        ICKCustomAttributeMultiProvider Attributes { get; }

        /// <summary>
        /// Gets kind of structure object for this StObj. It can be a <see cref="DependentItemKindSpec.Item"/>, 
        /// a <see cref="DependentItemKindSpec.Group"/> or a <see cref="DependentItemKindSpec.Container"/>.
        /// </summary>
        DependentItemKindSpec ItemKind { get; }

        /// <summary>
        /// Gets the parent <see cref="IStObjResult"/> in the inheritance chain (the one associated to the base class of this <see cref="ObjectType"/>).
        /// May be null.
        /// </summary>
        new IStObjResult Generalization { get; }

        /// <summary>
        /// Gets the child <see cref="IStObjResult"/> in the inheritance chain.
        /// May be null.
        /// </summary>
        new IStObjResult Specialization { get; }

        /// <summary>
        /// Gets the ultimate generalization <see cref="IStObjResult"/> in the inheritance chain. Never null (can be this object itself).
        /// </summary>
        IStObjResult RootGeneralization { get; }

        /// <summary>
        /// Gets the ultimate specialization <see cref="IStObjResult"/> in the inheritance chain. Never null (can be this object itself).
        /// </summary>
        IStObjResult LeafSpecialization { get; }

        /// <summary>
        /// Gets the configured container for this object. If this <see cref="Container"/> has been inherited 
        /// from its <see cref="Generalization"/>, this ConfiguredContainer is null.
        /// </summary>
        IStObjResult ConfiguredContainer { get; }

        /// <summary>
        /// Gets the container of this object. If no container has been explicitely associated for the object, this is the
        /// container of its <see cref="Generalization"/> (if it exists). May be null.
        /// </summary>
        IStObjResult Container { get; }

        /// <summary>
        /// Gets a list of required objects. This list combines the requirements of this items (explicitely required types, 
        /// construct parameters, etc.) and any RequiredBy from other objects.
        /// </summary>
        IReadOnlyList<IStObjResult> Requires { get; }

        /// <summary>
        /// Gets a list of Group objects to which this object belongs.
        /// </summary>
        IReadOnlyList<IStObjResult> Groups { get; }

        /// <summary>
        /// Gets a list of children objects when this <see cref="ItemKind"/> is either a <see cref="DependentItemKind.Group"/> or a <see cref="DependentItemKind.Container"/>.
        /// </summary>
        IReadOnlyList<IStObjResult> Children { get; }

        /// <summary>
        /// Gets the list of Ambient Properties that reference this object.
        /// </summary>
        IReadOnlyList<IStObjTrackedAmbientPropertyInfo> TrackedAmbientProperties { get; }

        /// <summary>
        /// Gets the value of the named property that may be associated to this StObj or to any StObj 
        /// in <see cref="Container"/> or <see cref="Generalization"/> 's chains (recursively).
        /// </summary>
        /// <param name="propertyName">Name of the property. Must not be null nor empty.</param>
        /// <returns>The property value (can be null) if the property has been defined, <see cref="Type.Missing"/> otherwise.</returns>
        object GetStObjProperty( string propertyName );
        
    }
}
