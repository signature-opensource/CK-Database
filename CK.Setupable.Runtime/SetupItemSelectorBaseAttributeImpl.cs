using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Base class to implement <see cref="SetupItemSelectorBaseAttribute"/> delegated attribute.
    /// This handles the lookup of the setup items and offers an alternative to <see cref="SetupObjectItemRefMemberAttributeImplBase"/> 
    /// to reference multiple items by their names.
    /// </summary>
    public abstract class SetupItemSelectorBaseAttributeImpl<T> : ISetupItemDriverAware where T : class
    {
        readonly SetupItemSelectorBaseAttribute _attribute;

        protected SetupItemSelectorBaseAttributeImpl( SetupItemSelectorBaseAttribute a )
        {
            _attribute = a;
        }

        /// <summary>
        /// Gets the actual attribute.
        /// </summary>
        protected SetupItemSelectorBaseAttribute Attribute => _attribute; 

        bool ISetupItemDriverAware.OnDriverPreInitialized( SetupItemDriver driver )
        {
            HashSet<string> already = new HashSet<string>();
            bool result = true;
            var items = new HashSet<ISortedItem>();
            foreach( var n in _attribute.CommaSeparatedTypeNames.Split( ',' ) )
            {
                string itemName = n.Trim();
                if( itemName.Length > 0 )
                {
                    if( already.Add( itemName ) )
                    {
                        IEnumerable<ISortedItem<ISetupItem>> namedItems = ItemsByName( driver, itemName );
                        int count = 0;
                        foreach( var i in namedItems )
                        {
                            ++count;
                            if( i.Item is T ) items.Add( i );
                            else 
                            {
                                driver.Engine.Monitor.Error( $"Item '{i.FullName}' in {_attribute.GetShortTypeName()} attribute of '{driver.Item.FullName}' must be a '{typeof(T).Name}'." );
                                result = false;
                            }
                        }
                        if( count == 0 )
                        {
                            driver.Engine.Monitor.Error( $"Name '{itemName}' in {_attribute.GetShortTypeName()} attribute of '{driver.Item.FullName}' not found." );
                            result = false;
                        }
                    }
                    else driver.Engine.Monitor.Warn( $"Duplicate name '{itemName}' in {_attribute.GetShortTypeName()} attribute of '{driver.Item.FullName}'." );
                }
            }
            if( !result ) return false;
            return OnDriverCreated( driver, items.OrderBy( s => s.Index ).Select( s => (T)s.Item ) );
        }

        /// <summary>
        /// Called once the driver of the object to which the attribute is applied has been created and 
        /// typed setup items have been selected based on their names.
        /// </summary>
        /// <param name="driver">The driver associated to the object to which the attribute is applied.</param>
        /// <param name="items">Selected items (in setup order).</param>
        /// <returns>True on success, false to stop the process.</returns>
        protected abstract bool OnDriverCreated( SetupItemDriver driver, IEnumerable<T> items );

        IEnumerable<ISortedItem<ISetupItem>> ItemsByName( SetupItemDriver driver, string name )
        {
            if( _attribute.SetupItemSelectorScope == SetupItemSelectorScope.DirectChildren )
            {
                return driver.SortedItem.Children.Where( c => c.FullName.Contains( name ) ).ToList();
            }
            else if( _attribute.SetupItemSelectorScope == SetupItemSelectorScope.All )
            {
                return driver.Engine.AllDrivers.Where( d => !d.IsGroupHead ).Select( d => d.SortedItem ).Where( c => c.FullName.Contains( name ) ).ToList();
            }
            Debug.Assert( _attribute.SetupItemSelectorScope == SetupItemSelectorScope.Children );
            return driver.SortedItem.AllChildren.Where( c => c.FullName.Contains( name ) ).ToList();
        }
    }
}
