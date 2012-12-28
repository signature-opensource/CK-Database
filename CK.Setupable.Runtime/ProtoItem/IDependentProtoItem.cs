using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Exposes a text version of the most versatile <see cref="IDependentItem"/> (the <see cref="IDependentItemContainerTyped"/>) with an additional 
    /// version (for a support of <see cref="IVersionedItem"/>). 
    /// All properties are optional (except <see cref="IContextLocNaming.FullName">FullName</see>, see remarks) and are mere strings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This captures information that can define (or partialy define) a dependent item (or be used to define/compose a dependent item) without beeing itself a <see cref="IDependentItem"/>
    /// nor a <see cref="IVersionedItem"/>.
    /// </para>
    /// <para>
    /// It seems coherent to consider FullName a nullable (ie. optional) property like the others since such proto item can be used as a partial definition. 
    /// Actually it is not nullable in order to extend from <see cref="IContextLocNaming"/> naming interface that requires its <see cref="IContextLocNaming.FullName"/> (and Name) to be not null.
    /// </para>
    /// <para>
    /// Extending IContextLocName makes this proto item simpler to understand and easier to work with.
    /// This should not be an issue (one can use a special FullName marker like "*" or "?" to handle this case - String.Empty may perfectly do the job if it has no semantics in the system).
    /// </para>
    /// </remarks>
    public interface IDependentProtoItem : IContextLocNaming
    {
        /// <summary>
        /// Gets the container name. Can be null.
        /// </summary>
        string Container { get; }

        /// <summary>
        /// Gets current version. 
        /// Null if no version exists or applies to this object.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets an identifier of the type of the item. This is required
        /// in order to be able to handle specific storage for version without 
        /// relying on any <see cref="FullName"/> conventions.
        /// Must be a non null, nor empty or whitespace identifier of at most 16 characters long.
        /// </summary>
        string ItemType { get; }
        
        /// <summary>
        /// Gets the name of the item that generalizes this one. 
        /// Null if this item does not specialize any other item.
        /// </summary>
        string Generalization { get; }

        /// <summary>
        /// Gets the kind of item. Can be <see cref="DependentItemKind.Unknown"/>.
        /// </summary>
        DependentItemKind ItemKind { get; }

        /// <summary>
        /// Gets the groups name to which this item belongs. Can be null.
        /// </summary>
        IEnumerable<string> Groups { get; }

        /// <summary>
        /// Gets the names of this item's dependencies. Can be null if no such dependency exists.
        /// </summary>
        IEnumerable<string> Requires { get; }

        /// <summary>
        /// Gets the names of the revert dependencies (an item can specify that it is itself required by another one). 
        /// A "RequiredBy" constraint is optional: a missing "RequiredBy" is not an error (it is considered 
        /// as a reverted optional dependency).
        /// Can be null if no such dependency exists.
        /// </summary>
        IEnumerable<string> RequiredBy { get; }

        /// <summary>
        /// Gets a list of children names. Can be null or empty.
        /// </summary>
        IEnumerable<string> Children { get; }

        /// <summary>
        /// Gets an optionnal list of <see cref="VersionedName"/>. <see cref="VersionedName.FullName"/> in this list
        /// are not null and the list is sorted by <see cref="VersionedName.Version"/> in ascending order.
        /// Can be null if no previous names exists.
        /// </summary>
        IEnumerable<VersionedName> PreviousNames { get; }

    }
}
