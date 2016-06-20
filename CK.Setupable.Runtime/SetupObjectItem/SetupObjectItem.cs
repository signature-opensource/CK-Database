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
        readonly string _type;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        IDependentItemContainerRef _container;
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

        public IContextLocNaming ContextLocName => _contextLocName; 

        public IDependentItemList Requires => _requires ?? (_requires = new DependentItemList()); 

        public IDependentItemList RequiredBy => _requiredBy ?? (_requiredBy = new DependentItemList()); 

        public IDependentItemGroupList Groups => _groups ?? (_groups = new DependentItemGroupList()); 

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _container.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _contextLocName.Context, _contextLocName.Location ) ); }
        }

        IDependentItemRef IDependentItem.Generalization => null; 

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
        /// Gets the type of the object ("Procedure" for instance). This implements the <see cref="IVersionedItem.ItemType"/>.
        /// </summary>
        public string ItemType => _type; 

        string IContextLocNaming.Context => _contextLocName.Context; 

        string IContextLocNaming.Location => _contextLocName.Location;

        string IContextLocNaming.TransformArg => _contextLocName.TransformArg;

        string IContextLocNaming.Name => _contextLocName.Name;

        /// <summary>
        /// Gets the full name of this object.
        /// </summary>
        public string FullName => _contextLocName.FullName;

        bool IDependentItemRef.Optional => false; 

        object IDependentItem.StartDependencySort() => StartDependencySort();

        /// <summary>
        /// Abstract method that is called at the beginning of the topological sort.
        /// </summary>
        /// <returns>An object (a state) that will be associated to this item.</returns>
        protected abstract object StartDependencySort();

    }

}
