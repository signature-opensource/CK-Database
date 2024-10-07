using System.Collections.Generic;
using CK.Core;

namespace CK.Setup;

/// <summary>
/// A setup object item implementation for items that can be containers or groups 
/// but when version does not apply.
/// </summary>
public abstract class SetupObjectItemC : SetupObjectItem, IDependentItemContainerTyped, IDependentItemContainerRef
{
    IDependentItemList _children;

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
    protected SetupObjectItemC( ContextLocName name, string itemType )
        : base( name, itemType )
    {
    }

    /// <summary>
    /// Gets the transform target item if this item has associated <see cref="SetupObjectItem.Transformers"/>.
    /// This object is created as a clone of this object by the first call 
    /// to this <see cref="SetupObjectItem.AddTransformer"/> method.
    /// </summary>
    public new SetupObjectItemC TransformTarget => (SetupObjectItemC)base.TransformTarget;

    /// <summary>
    /// Called by <see cref="SetupObjectItem.AddTransformer"/> to initialize the initial 
    /// transform target as a clone of this object.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on success, false if an error occured.</returns>
    protected override bool OnTransformTargetCreated( IActivityMonitor monitor )
    {
        if( !base.OnTransformTargetCreated( monitor ) ) return false;
        // Should the transformed item be the container of the non transformed one?
        // I guess no.
        // if( _children != null ) TransformTarget._children = DependentItemListFactory.CreateDependentItemList( _children );
        _children = null;
        return true;
    }

    /// <summary>
    /// Gets the mutable list of children.
    /// </summary>
    public IDependentItemList Children => _children ?? (_children = DependentItemListFactory.CreateItemList());

    /// <summary>
    /// Gets or sets the kind of item. Can be <see cref="DependentItemKind.Unknown"/>.
    /// </summary>
    public DependentItemKind ItemKind { get; set; }

    IEnumerable<IDependentItemRef> IDependentItemGroup.Children
    {
        get { return _children.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, ContextLocName.Context, ContextLocName.Location ) ); }
    }
}
