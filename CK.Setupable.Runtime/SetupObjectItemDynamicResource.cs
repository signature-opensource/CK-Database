using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup;


/// <summary>
/// Tracks the initialization of a setup item from resource.
/// This object is used for both the key and the value in the <see cref="IStObjSetupDynamicInitializerState.Memory"/>.
/// This secures the key since only this SetupObjectItemDynamicResource can be equal to a SetupObjectItemDynamicResource.
/// </summary>
public class SetupObjectItemDynamicResource
{
    readonly int _hash;
    readonly string _fullName;
    SetupObjectItem _item;

    internal SetupObjectItemDynamicResource( IContextLocNaming name )
        : this( name.FullName )
    {
        Name = name;
    }

    /// <summary>
    /// Constructor for the key lookup.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    internal SetupObjectItemDynamicResource( string fullName )
    {
        _fullName = fullName;
        _hash = _fullName.GetHashCode();
    }

    /// <summary>
    /// Overridden to handle equality based on the <see cref="Name"/>.
    /// </summary>
    /// <param name="obj">The other object.</param>
    /// <returns>True if equals, false otherwise.</returns>
    public override bool Equals( object obj )
    {
        SetupObjectItemDynamicResource x = obj as SetupObjectItemDynamicResource;
        return x != null && x._fullName == _fullName;
    }

    /// <summary>
    /// Gets the hash code (the <see cref="Name"/>'s one).
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => _hash;

    /// <summary>
    /// Gets the name of the item.
    /// </summary>
    public IContextLocNaming Name { get; }

    /// <summary>
    /// Gets the eventually created item.
    /// </summary>
    public SetupObjectItem Item
    {
        get => _item;
        internal set
        {
            Debug.Assert( _item == null );
            if( value != null )
            {
                _item = value;
                ItemAvailable?.Invoke( this, EventArgs.Empty );
            }
        }
    }

    /// <summary>
    /// Fires whenever the Item is available.
    /// </summary>
    public event EventHandler ItemAvailable;

    /// <summary>
    /// Registers or immediately executes an action that handles <see cref="Item"/>.
    /// </summary>
    /// <param name="m">The monitor to use.</param>
    /// <param name="a">The action to call.</param>
    public void OnItemAvailable( IActivityMonitor m, Action<IActivityMonitor, SetupObjectItem> a )
    {
        if( _item != null ) a( m, _item );
        else ItemAvailable += ( o, e ) => a( m, _item );
    }

    /// <summary>
    /// The last definer is the winner.
    /// </summary>
    internal IStObjSetupDynamicInitializer LastDefiner;

    /// <summary>
    /// Keeps the container that has the first definer.
    /// </summary>
    internal IMutableSetupItem FirstContainer;

    /// <summary>
    /// Keeping the last container is used to handle multiple definitions
    /// in the same container.
    /// </summary>
    internal IMutableSetupItem LastContainerSeen;

    /// <summary>
    /// The transform target creator in Name has a transform argument.
    /// </summary>
    public SetupObjectItemDynamicResource TransformTarget;

    /// <summary>
    /// Overridden to return the <see cref="Name"/> of this item.
    /// </summary>
    /// <returns>The item's name.</returns>
    public override string ToString() => Name.ToString();
}
