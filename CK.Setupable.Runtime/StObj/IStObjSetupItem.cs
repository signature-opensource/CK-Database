using CK.Core;

namespace CK.Setup;

/// <summary>
/// A setup item that is bound to a StObj.
/// </summary>
public interface IStObjSetupItem : IMutableSetupItem, ISetupObjectItem
{
    /// <summary>
    /// Gets the StObj. Null if this item is directly bound to an object.
    /// </summary>
    IStObjResult StObj { get; }

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
    bool SetDirectPropertyValue( IActivityMonitor monitor, string propertyName, object value, string sourceDescription = null );

}
