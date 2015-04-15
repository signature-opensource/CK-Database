using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup object item is initialized from a <see cref="ISetupObjectProtoItem"/>.
    /// This is the implementation for items that can be containers or groups but when version does not apply.
    /// </summary>
    public abstract class SetupObjectItemC : SetupObjectItem, IDependentItemContainerTyped, IDependentItemContainerRef
    {
        DependentItemKind _itemKind;
        DependentItemList _children;

        protected SetupObjectItemC( ISetupObjectProtoItem p )
            : base( p )
        {
            _itemKind = p.ItemKind;
            if( p.Children != null ) Children.Add( p.Children );
        }

        /// <summary>
        /// Gets or sets the object that replaces this object.
        /// </summary>
        public new SetupObjectItemC ReplacedBy
        {
            get { return (SetupObjectItemC)base.ReplacedBy; }
            internal protected set { base.ReplacedBy = value; }
        }

        /// <summary>
        /// Gets the object that is replaced by this one.
        /// </summary>
        public new SetupObjectItemC Replaces
        {
            get { return (SetupObjectItemC)base.Replaces; }
        }

        public IDependentItemList Children
        {
            get { return _children ?? (_children = new DependentItemList()); }
        }

        /// <summary>
        /// Gets or sets the kind of item.
        /// </summary>
        public DependentItemKind ItemKind
        {
            get { return _itemKind; }
            set { _itemKind = value; }
        }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, ContextLocName.Context, ContextLocName.Location ) ); }
        }
    }



}
