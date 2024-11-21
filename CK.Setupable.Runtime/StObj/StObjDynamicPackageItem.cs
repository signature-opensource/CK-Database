using System.Diagnostics;
using CK.Core;

namespace CK.Setup;

/// <summary>
/// Default <see cref="IMutableSetupItemContainer"/> implementation associated to <see cref="IStObjResult"/> object.
/// Used when the <see cref="IStObjSetupData"/> does not specify a dedicated implementation (<see cref="IStObjSetupData.ItemType"/> 
/// nor <see cref="IStObjSetupData.ItemTypeName"/> are set).
/// This class can (and should) be used as a base class for more specific item implementation.
/// </summary>
public class StObjDynamicPackageItem : DynamicPackageItem, IStObjSetupItem, ISetupObjectItem
{
    readonly IStObjSetupData _setupData;

    /// <summary>
    /// Initializes a new <see cref="StObjDynamicPackageItem"/> initialized by a <see cref="IStObjSetupData"/>.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="data">Descriptive data that is used to configure this item.</param>
    public StObjDynamicPackageItem( IActivityMonitor monitor, IStObjSetupData data )
        : base( data.StObj.ItemKind == DependentItemKindSpec.Item ? "StObjItem" : "StObjPackage", (object)data.DriverType ?? data.DriverTypeName )
    {
        Debug.Assert( ModelPackage == null, "Initially, a DynamicPackageItem has no Model." );
        Debug.Assert( ObjectsPackage == null, "Initially, a DynamicPackageItem has no ObjectsPackage." );
        Debug.Assert( data.ItemType == null || typeof( StObjDynamicPackageItem ).IsAssignableFrom( data.ItemType ), "If we are using a StObjDynamicPackageItem, this is because no explicit ItemType (nor ItemTypeName) have been set, or it is a type that specializes this." );
        ItemKind = (DependentItemKind)data.StObj.ItemKind;
        SetVersionsString( data.Versions );
        _setupData = data;
        FullName = data.FullName;
        Requires.AddRange( data.Requires );
        RequiredBy.AddRange( data.RequiredBy );
        Groups.AddRange( data.Groups );
        Children.AddRange( data.Children );
    }

    /// <summary>
    /// Gets the StObj.
    /// </summary>
    public IStObjResult StObj => _setupData.StObj;

    /// <summary>
    /// Gets the associated object instance (the final, most specialized, structured object). 
    /// </summary>
    public object ActualObject => _setupData.StObj.FinalImplementation.Implementation;


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
        return _setupData.SetDirectPropertyValue( monitor, propertyName, value, sourceDescription );
    }


}
