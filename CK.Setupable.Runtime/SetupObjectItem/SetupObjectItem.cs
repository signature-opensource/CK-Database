using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup object item is typically an item that originates from an attribute or a StObj member 
    /// and initialized from a <see cref="ISetupObjectProtoItem"/>.
    /// </summary>
    public abstract class SetupObjectItem : ISetupItem, IDependentItemRef
    {
        string _type;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        IDependentItemContainerRef _container;
        SetupObjectItem _replacedBy;
        SetupObjectItem _replaces;
        IContextLocNaming _contextLocName;

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItem"/> from a <see cref="ISetupObjectProtoItem"/>.
        /// </summary>
        /// <param name="p"></param>
        protected SetupObjectItem( ISetupObjectProtoItem p )
        {
            _contextLocName = p.ContextLocName;
            _type = p.ItemType;
            if( p.Requires != null ) Requires.Add( p.Requires );
            if( p.RequiredBy != null ) RequiredBy.Add( p.RequiredBy );
            if( p.Groups != null ) Groups.Add( p.Groups );
            // If the proto object indicates a container, references it: its name will be
            // used by the dependency sorter.
            // If it is not the same as the actual container to which this object
            // is added later, an error will be raised during the ordering. 
            if( p.Container != null ) _container = new NamedDependentItemContainerRef( p.Container );
        }

        /// <summary>
        /// Gets or sets the object that replaces this object.
        /// </summary>
        public SetupObjectItem ReplacedBy
        {
            get { return _replacedBy; }
            internal protected set
            {
                if( _replacedBy != null ) _replacedBy._replaces = null;
                _replacedBy = value;
                if( _replacedBy != null ) _replacedBy._replaces = this;
            }
        }

        /// <summary>
        /// Gets the object that is replaced by this one.
        /// </summary>
        public SetupObjectItem Replaces
        {
            get { return _replaces; }
        }

        public IContextLocNaming ContextLocName
        {
            get { return _contextLocName; }
        }

        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        public IDependentItemGroupList Groups
        {
            get { return _groups ?? (_groups = new DependentItemGroupList()); }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _container.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _contextLocName.Context, _contextLocName.Location ) ); }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return null; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _contextLocName.Context, _contextLocName.Location ) ); }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _contextLocName.Context, _contextLocName.Location ) ); }
        }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get { return _groups.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _contextLocName.Context, _contextLocName.Location ) ); }
        }

        /// <summary>
        /// Gets the type of the object ("Procedure" for instance). This implements the <see cref="IVersionedItem.Type"/>.
        /// </summary>
        public string ItemType
        {
            get { return _type; }
        }

        string IContextLocNaming.Context
        {
            get { return _contextLocName.Context; }
        }

        string IContextLocNaming.Location
        {
            get { return _contextLocName.Location; }
        }

        string IContextLocNaming.Name
        {
            get
            {
                if( _replaces != null ) return _contextLocName.Name + "#replace";
                return _contextLocName.Name;
            }
        }

        /// <summary>
        /// Gets the full name of this object.
        /// </summary>
        public string FullName
        {
            get
            {
                if( _replaces != null ) return _contextLocName.FullName + "#replace";
                return _contextLocName.FullName;
            }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        object IDependentItem.StartDependencySort()
        {
            return StartDependencySort();
        }

        protected abstract object StartDependencySort();

    }

}
