using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup object item implementation for items that can be containers or groups 
    /// but when version does not apply.
    /// </summary>
    public abstract class SetupObjectItemC : SetupObjectItem, IDependentItemContainerTyped, IDependentItemContainerRef
    {
        DependentItemList _children;

        /// <summary>
        /// Initializes a <see cref="SetupObjectItemC"/> without ContextLocName nor ItemType.
        /// Specialized class must take care of initializing them: having no name nor type is not valid.
        /// </summary>
        protected SetupObjectItemC()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemC"/>.
        /// </summary>
        /// <param name="name">Initial name of this item. Can not be null.</param>
        /// <param name="itemType">Type of the item. Can not be null nor longer than 16 characters.</param>
        /// <param name="containerName">
        /// Optional container name to which this item belongs. Its name will be used by the dependency sorter.
        /// If it is not the same as the actual container to which this object
        /// is added later, an error will be raised during the ordering. 
        /// </param>
        protected SetupObjectItemC( ContextLocName name, string itemType, string containerName = null )
            : base( name, itemType, containerName )
        {
        }

        /// <summary>
        /// Gets the mutable list of children.
        /// </summary>
        public IDependentItemList Children => _children ?? (_children = new DependentItemList());

        /// <summary>
        /// Gets or sets the kind of item. Can be <see cref="DependentItemKind.Unknown"/>.
        /// </summary>
        public DependentItemKind ItemKind { get; set; }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, ContextLocName.Context, ContextLocName.Location ) ); }
        }
    }



}
