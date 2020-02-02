using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Default <see cref="IMutableSetupItemContainer"/> implementation associated to <see cref="IStObjResult"/> object.
    /// Used when the <see cref="IStObjSetupData"/> does not specify a dedicated implementation (<see cref="IStObjSetupData.ItemType"/> 
    /// nor <see cref="IStObjSetupData.ItemTypeName"/> are set).
    /// This class can (and should) be used as a base class for more specific item implementation.
    /// </summary>
    public class StObjDynamicContainerItem : DynamicContainerItem, IStObjSetupItem
    {
        readonly IStObjSetupData _data;

        /// <summary>
        /// Initializes a new <see cref="StObjDynamicContainerItem"/> initialized by a <see cref="IStObjSetupData"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Descriptive data that is used to configure this item.</param>
        /// <param name="defaultDriverType">Can be a string or a Type: if <see cref="IStObjSetupData.DriverType"/> and <see cref="IStObjSetupData.DriverTypeName"/> is null, this driver is used.</param>
        public StObjDynamicContainerItem( IActivityMonitor monitor, IStObjSetupData data, object defaultDriverType = null )
            : base( data.DriverType ?? data.DriverTypeName ?? defaultDriverType )
        {
            Debug.Assert( data.ItemType == null || typeof( StObjDynamicContainerItem ).IsAssignableFrom( data.ItemType ), "If we are using a StObjDynamicContainerItem, this is because no explicit ItemType (nor ItemTypeName) have been set, or it is a type that specializes this." );
            ItemKind = (DependentItemKind)data.StObj.ItemKind;
            _data = data;
            FullName = data.FullName;
            Requires.AddRange( data.Requires );
            RequiredBy.AddRange( data.RequiredBy );
            Groups.AddRange( data.Groups );
            Children.AddRange( data.Children );
        }

        /// <summary>
        /// Gets the StObj. Null if this item is directly bound to an object.
        /// </summary>
        public IStObjResult StObj => _data.StObj; 
        
        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object).
        /// </summary>
        public object ActualObject => _data.StObj.InitialObject;

        /// <summary>
        /// Sets a direct property (it must not be an Ambient Property, Singleton nor a StObj property) on the Structured Object. 
        /// The property must exist, be writable and the type of the <paramref name="value"/> must be compatible with the property type 
        /// otherwise an error is logged.
        /// </summary>
        /// <param name="monitor">The monitor to use to describe any error.</param>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="sourceDescription">Optional description of the origin of the value to help troubleshooting.</param>
        /// <returns>True on success, false if any error occurs.</returns>
        public bool SetDirectPropertyValue( IActivityMonitor monitor, string propertyName, object value, string sourceDescription = null )
        {
            return _data.SetDirectPropertyValue( monitor, propertyName, value, sourceDescription );
        }



    }
}
