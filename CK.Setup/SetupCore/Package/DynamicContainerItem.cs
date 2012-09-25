using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup
{
    public class DynamicContainerItem : IDependentItemContainerAsk, IDependentItemContainerRef
    {
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemList _children;
        IDependentItemContainerRef _container;
        IDependentItemRef _generalization;
        string _fullName;

        public DynamicContainerItem()
        {
        }

        /// <summary>
        /// Gets or sets the full name of this container.
        /// It must be not null.
        /// </summary>
        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value; }
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
        /// Gets or sets whether this container is actually NOT a container.
        /// When set to true, if this container contains children or if an item declares this
        /// item as its container, an error is raised during the ordering of the dependency graph.
        /// </summary>
        public bool ThisIsNotAContainer { get; set; }
        
        /// <summary>
        /// Called at the very beginning of the setup phasis, before <see cref="IDependentItem.FullName"/> is used to planify the setup. 
        /// This start method has been already called on direct dependencies <see cref="Container"/> 
        /// and <see cref="Requires"/> if they are <see cref="IDependentItem"/> (and not strings).
        /// </summary>
        /// <returns>
        /// Must return the <see cref="Type"/> of the setup driver (specialization of <see cref="DriverBase"/>), or its assembly qualified name.
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

