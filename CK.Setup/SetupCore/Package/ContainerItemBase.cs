using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Implementation of a mutable <see cref="IDependentItemContainer"/> that is NOT a <see cref="IVersionedItem"/>.
    /// This class implements the minimal behavior for an item container: FullName, Requires, RequiredBy, Container, Generalization and Children, (plus 
    /// the <see cref="IDependentItemContainerAsk.ThisIsNotAContainer"/> that can be used to dynamically refuse to be referenced
    /// as a Container by other items).
    /// All of them are writable except FullName and ThisIsNotAContainer that must be provided through implementations of abstract methods.
    /// </summary>
    /// <remarks>
    /// The <see cref="PackageItemBase"/> must be used for container that have multiple versions and an optional associated model.
    /// </remarks>
    public abstract class ContainerItemBase : IMutableDependentItem, IDependentItemContainerAsk, IDependentItemContainerRef
    {
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemList _children;
        IDependentItemContainerRef _container;
        IDependentItemRef _generalization;

        public ContainerItemBase()
        {
        }

        /// <summary>
        /// Gets a mutable list of items that this container requires.
        /// </summary>
        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        /// <summary>
        /// Gets a mutable list of items that are required by this container.
        /// </summary>
        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        /// <summary>
        /// Gets or sets the container to which this container belongs.
        /// </summary>
        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

        /// <summary>
        /// Gets or sets the generalization of this container.
        /// </summary>
        public IDependentItemRef Generalization
        {
            get { return _generalization; }
            set { _generalization = value; }
        }

        /// <summary>
        /// Gets a mutable list of children for this package.
        /// </summary>
        public IDependentItemList Children
        {
            get { return _children ?? (_children = new DependentItemList()); }
        }

        /// <summary>
        /// Must return the full name of this item.
        /// It can be computed by <see cref="StartDependencySort"/>.
        /// </summary>
        /// <returns>This full name.</returns>
        protected abstract string GetFullName();

        /// <summary>
        /// Gets whether this container is actually NOT a container.
        /// When true, if an item declares this item as its container, an error is 
        /// raised during the ordering of the dependency graph.
        /// </summary>
        protected abstract bool GetThisIsNotAContainer();

        /// <summary>
        /// Called at the very beginning of the setup phasis, before <see cref="IDependentItem.FullName"/> is used to planify the setup. 
        /// This start method has been already called on direct dependencies <see cref="Container"/>, <see cref="Generalization"/>
        /// and <see cref="Requires"/> if they are <see cref="IDependentItem"/> (and not strings).
        /// </summary>
        /// <returns>
        /// Must return the <see cref="Type"/> of the setup driver (specialization of <see cref="SetupDriver"/>), or its assembly qualified name.
        /// By default, returns the type of <see cref="SetupDriver"/>.
        /// </returns>
        protected virtual object StartDependencySort()
        {
            return typeof( SetupDriver );
        }

        object IDependentItem.StartDependencySort()
        {
            return StartDependencySort();
        }

        string IDependentItem.FullName
        {
            get { return GetFullName(); }
        }

        bool IDependentItemContainerAsk.ThisIsNotAContainer
        {
            get { return GetThisIsNotAContainer(); }
        }

        string IDependentItemRef.FullName
        {
            get { return GetFullName(); }
        }
        
        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy; }
        }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children; }
        }
    }


}

