using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup object item is typically an item that originates from an attribute or a StObj member.
    /// </summary>
    public abstract class SetupObjectItem : IMutableSetupBaseItem, IDependentItemRef, IDependentItemDiscoverer<ISetupItem>
    {
        string _itemType;
        ContextLocName _contextLocName;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        SetupObjectItem _transformTarget;
        List<ISetupObjectTransformerItem> _transformers;
        SetupObjectItem _sourceWhenTransformed;
        object _driverType;

        /// <summary>
        /// Initializes a <see cref="SetupObjectItem"/> without <see cref="ContextLocName"/> nor <see cref="ItemType"/>.
        /// Specialized class must take care of initializing them: having no name nor type is not valid.
        /// </summary>
        protected SetupObjectItem()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItem"/>.
        /// </summary>
        /// <param name="name">Initial name of this item. Can not be null.</param>
        /// <param name="itemType">Type of the item. Can not be null nor longer than 16 characters.</param>
        protected SetupObjectItem( ContextLocName name, string itemType )
        {
            ContextLocName = name;
            ItemType = itemType;
        }

        /// <summary>
        /// Gets the transform target item if this item has associated <see cref="Transformers"/>.
        /// This object is created as a clone of this object by the first call 
        /// to this <see cref="AddTransformer"/> method.
        /// </summary>
        public SetupObjectItem TransformTarget => _transformTarget;

        /// <summary>
        /// Gets the source item if this item is a target, null otherwise.
        /// </summary>
        public SetupObjectItem TransformSource => _sourceWhenTransformed;

        /// Gets or sets whether explicit requirements to objects applies to their 
        /// eventual transformed object if any.
        /// </summary>
        public bool ExplicitRequiresMustBeTransformed { get; set; }

        /// <summary>
        /// Gets the transformers that have been registered with <see cref="AddTransformer"/>.
        /// Never null (empty when no transformers have been added yet).
        /// </summary>
        public IReadOnlyList<ISetupObjectTransformerItem> Transformers => (IReadOnlyList<ISetupObjectTransformerItem>)_transformers ?? Util.Array.Empty<ISetupObjectTransformerItem>();

        class DownCastList<T> : IReadOnlyList<T>
        {
            readonly SetupObjectItem _holder;

            public DownCastList( SetupObjectItem holder )
            {
                _holder = holder;
            }

            public T this[int index] => (T)_holder.Transformers[index];

            public int Count => _holder.Transformers.Count;

            public IEnumerator<T> GetEnumerator() => _holder.Transformers.Cast<T>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _holder.Transformers.GetEnumerator();
        }

        /// <summary>
        /// Helper for specialized class that helps covariant interface implementation.
        /// </summary>
        /// <typeparam name="T">The actual type of the trasformer to expose.</typeparam>
        /// <returns>A wrapper around <see cref="Transformers"/> that downcasts its items.</returns>
        protected IReadOnlyList<T> CreateTypedTransformersWrapper<T>() => new DownCastList<T>( this ); 

        /// <summary>
        /// Adds a <see cref="ISetupObjectTransformerItem"/> transformers.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="transformer">The transformer to add and configure.</param>
        /// <returns>The <see cref="TransformTarget"/> object or null on error.</returns>
        public SetupObjectItem AddTransformer( IActivityMonitor monitor, ISetupObjectTransformerItem transformer )
        {
            if( transformer == null ) throw new ArgumentNullException( nameof( transformer ) );
            if( transformer.Source != null || transformer.Target != null )
            {
                monitor.Error().Send( $"Transformer {transformer.FullName} is already bound to a source ({transformer.Source?.FullName}) and/or to a target ({transformer.Target?.FullName})", nameof( transformer ) );
                return null;
            }
            if( _transformTarget == null )
            {
                _transformTarget = (SetupObjectItem)MemberwiseClone();
                _transformTarget._sourceWhenTransformed = this;
                OnTransformTargetCreated( monitor );
                _transformers = new List<ISetupObjectTransformerItem>();
            }
            transformer.Source = this;
            transformer.Target = _transformTarget;
            _transformers.Add( transformer );
            transformer.Requires.Add( this );
            TransformTarget.Requires.Add( transformer.GetReference() );
            return TransformTarget;
        }

        /// <summary>
        /// Called by <see cref="AddTransformer"/> to initialize the initial 
        /// transform target as a clone of this object.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false if an error occured.</returns>
        protected virtual bool OnTransformTargetCreated( IActivityMonitor monitor )
        {
            _transformTarget._contextLocName = _contextLocName.Clone();
            _transformTarget._contextLocName.Name += "#transform";

            // This new target requires its source (its cloned origin), therefore it is useless 
            // to duplicate its requires.
            // This is obvious.
            //if( _requires != null ) _transformTarget._requires = new DependentItemList( _requires );
            _transformTarget._requires = null;

            // If the source object defines any RequiredBy, it is up to the transformation 
            // to also define them.
            // This is a less obvious choice as the previous one.
            //if( _requiredBy != null ) _transformTarget._requiredBy = new DependentItemList( _requiredBy );
            _transformTarget._requiredBy = null;

            // Same consideration here: it is up to the transformation to consider the transformed item
            // to be in the same groups as the original.
            // This choice, as well as the following one regarding the container, is the most 
            // questionable one.
            //if( _groups != null ) _transformTarget._groups = new DependentItemGroupList( _groups );
            _transformTarget._groups = null;

            // Here, we free the transformed object to be in the same container as its original one.
            // This is clearly a choice that can be discussed...
            _transformTarget.Container = null;
            return true;
        }

        /// <summary>
        /// Gets or sets the name of this object. Must not be null.
        /// </summary>
        public ContextLocName ContextLocName
        {
            get { return _contextLocName; }
            set
            {
                if( value == null ) throw new ArgumentNullException( nameof( ContextLocName ) );
                _contextLocName = value;
            }
        }

        /// <summary>
        /// Gets or sets the container to which this item belongs.
        /// </summary>
        public IDependentItemContainerRef Container { get; set; }

        /// <summary>
        /// Gets the mutable list of requirements for this item.
        /// </summary>
        public IDependentItemList Requires => _requires ?? (_requires = new DependentItemList());

        /// <summary>
        /// Gets the mutable list or reverse requirements for this item.
        /// </summary>
        public IDependentItemList RequiredBy => _requiredBy ?? (_requiredBy = new DependentItemList());

        /// <summary>
        /// Gets the mutable list or groups to which this item belongs.
        /// </summary>
        public IDependentItemGroupList Groups => _groups ?? (_groups = new DependentItemGroupList());

        /// <summary>
        /// Gets or sets the type of the object ("Procedure" for instance). 
        /// </summary>
        /// <summary>
        /// Gets an identifier of the type of the item. This is required
        /// in order to be able to handle specific storage for version without 
        /// relying on any <see cref="IContextLocNaming.FullName">FullName</see> conventions.
        /// Must be a non null, nor empty or whitespace identifier of at most 16 characters long.
        /// Moreover this can be used by implementations to "type" objects based on their actual content: this "content-based" type can then be
        /// checked later (like "the content is a function whereas a procedure is expected.").
        /// This implements the <see cref="IVersionedItem.ItemType"/>.
        /// </summary>
        public string ItemType
        {
            get { return _itemType; }
            set
            {
                if( string.IsNullOrWhiteSpace( value ) || value.Length > 16 ) throw new ArgumentException( "Invalid type.", nameof(ItemType) );
                _itemType = value;
            }
        }


        /// <summary>
        /// Gets the full name of this object.
        /// </summary>
        public string FullName => _contextLocName.FullName;

        /// <summary>
        /// Sets the assembly quelified name of the <see cref="SetupItemDriver"/> object
        /// to use for setup.
        /// </summary>
        /// <param name="assemblyQualifiedName">Assembly qualified name.</param>
        public void SetDriverType( string assemblyQualifiedName ) => _driverType = assemblyQualifiedName;

        /// <summary>
        /// Sets the type of the <see cref="SetupItemDriver"/> object
        /// to use for setup.
        /// </summary>
        /// <param name="driverType">Type of the driver.</param>
        public void SetDriverType( Type driverType ) => _driverType = driverType;


        /// <summary>
        /// Virtual method that is called at the beginning of the topological sort.
        /// <see cref="SetDriverType(string)"/> or <see cref="SetDriverType(Type)"/> must have been
        /// called with a non null driver type before this step otherwise an <see cref="InvalidOperationException"/>
        /// is thrown.
        /// </summary>
        /// <returns>
        /// Must return the <see cref="Type"/> or the Assembly qualified name of the 
        /// associated <see cref="SetupItemDriver"/> for this item.
        /// </returns>
        protected virtual object StartDependencySort()
        {
            if( _driverType == null ) throw new InvalidOperationException( "SetDriverType must have been called before starting the topological sort." );
            return _driverType;
        }

        #region Explicit implementations of IContextLocNaming (to avoid clutering the interface).

        string IContextLocNaming.Context => _contextLocName.Context; 

        string IContextLocNaming.Location => _contextLocName.Location;

        string IContextLocNaming.TransformArg => _contextLocName.TransformArg;

        string IContextLocNaming.Name => _contextLocName.Name;

        #endregion

        #region Explicit implementation of IDependentItem a IDependentItemRef (used by the DependencySorter).
        IDependentItemContainerRef IDependentItem.Container
        {
            get { return Container.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _contextLocName.Context, _contextLocName.Location ) ); }
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

        bool IDependentItemRef.Optional => false; 

        object IDependentItem.StartDependencySort()
        {
            if( _requires != null && ExplicitRequiresMustBeTransformed )
            {
                for( int i = 0; i < _requires.Count; ++i )
                {
                    var transformed = (_requires[i] as SetupObjectItem)?.TransformTarget;
                    if( transformed != null ) _requires[i] = transformed;
                }
            }
            return StartDependencySort();
        }
        /// <summary>
        /// This ensures that the transform target is registered if it exists
        /// even if requirements do not start from referenced objects.
        /// </summary>
        /// <returns>The transform target if it exists.</returns>
        IEnumerable<ISetupItem> IDependentItemDiscoverer<ISetupItem>.GetOtherItemsToRegister()
        {
            return _transformTarget != null ? new[] { _transformTarget } : null;
        }

        #endregion
    }

}
