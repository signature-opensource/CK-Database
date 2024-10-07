using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.Core;

namespace CK.Setup;

/// <summary>
/// Base class to implement <see cref="SetupItemSelectorBaseAttribute"/> delegated attribute.
/// This handles the lookup of the setup items and offers an alternative to <see cref="SetupObjectItemRefMemberAttributeImplBase"/> 
/// to reference multiple items by their names.
/// </summary>
public abstract class SetupItemSelectorBaseAttributeImpl<T> : ISetupItemDriverAware where T : class
{
    readonly SetupItemSelectorBaseAttribute _attribute;

    /// <summary>
    /// Initializes a new <see cref="SetupItemSelectorBaseAttributeImpl{T}"/>.
    /// </summary>
    /// <param name="a">The attribute.</param>
    protected SetupItemSelectorBaseAttributeImpl( SetupItemSelectorBaseAttribute a )
    {
        _attribute = a;
    }

    /// <summary>
    /// Gets the actual attribute.
    /// </summary>
    protected SetupItemSelectorBaseAttribute Attribute => _attribute;

    bool ISetupItemDriverAware.OnDriverPreInitialized( IActivityMonitor monitor, SetupItemDriver driver )
    {
        HashSet<string> already = new HashSet<string>();
        bool result = true;
        var items = new HashSet<ISortedItem>();
        foreach( var n in _attribute.CommaSeparatedTypeNames.Split( ',' ) )
        {
            string rawName = n.Trim();
            if( rawName.Length > 0 )
            {
                if( already.Add( rawName ) )
                {
                    var itemName = driver.Item.CombineName( rawName );
                    IEnumerable<ISortedItem<ISetupItem>> namedItems = ItemsByName( driver, itemName );
                    int count = 0;
                    foreach( var i in namedItems )
                    {
                        ++count;
                        if( i.Item is T ) items.Add( i );
                        else
                        {
                            monitor.Error( $"Item '{i.FullName}' in {_attribute.GetShortTypeName()} attribute of '{driver.Item.FullName}' must be a '{typeof( T ).Name}'." );
                            result = false;
                        }
                    }
                    if( count == 0 )
                    {
                        monitor.Error( $"Name '{rawName}' in {_attribute.GetShortTypeName()} attribute of '{driver.Item.FullName}' not found." );
                        result = false;
                    }
                }
                else monitor.Warn( $"Duplicate name '{rawName}' in {_attribute.GetShortTypeName()} attribute of '{driver.Item.FullName}'." );
            }
        }
        if( !result ) return false;
        return OnDriverCreated( monitor, driver, items.OrderBy( s => s.Index ).Select( s => (T)s.Item ) );
    }

    /// <summary>
    /// Called once the driver of the object to which the attribute is applied has been created and 
    /// typed setup items have been selected based on their names.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="driver">The driver associated to the object to which the attribute is applied.</param>
    /// <param name="items">Selected items (in setup order).</param>
    /// <returns>True on success, false to stop the process.</returns>
    protected abstract bool OnDriverCreated( IActivityMonitor monitor, SetupItemDriver driver, IEnumerable<T> items );

    IEnumerable<ISortedItem<ISetupItem>> ItemsByName( SetupItemDriver driver, IContextLocNaming itemName )
    {
        if( _attribute.SetupItemSelectorScope == SetupItemSelectorScope.DirectChildren )
        {
            return driver.SortedItem.Children.Where( c => c.FullName == itemName.FullName ).ToList();
        }
        else if( _attribute.SetupItemSelectorScope == SetupItemSelectorScope.All )
        {
            return driver.Drivers.Where( d => d.FullName == itemName.FullName ).Select( d => d.SortedItem ).ToList();
        }
        Debug.Assert( _attribute.SetupItemSelectorScope == SetupItemSelectorScope.Children );
        return driver.SortedItem.GetAllChildren().Where( c => c.FullName == itemName.FullName );
    }
}
