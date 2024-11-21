namespace CK.Setup;

/// <summary>
/// This interface exposes a <see cref="SetupObjectItem"/>. 
/// It is implemented by <see cref="SetupObjectItemMemberAttributeImplBase"/> so that other attributes on the member 
/// can interact with the item. 
/// </summary>
public interface ISetupObjectItemProvider
{
    /// <summary>
    /// Gets the <see cref="SetupObjectItem"/>.
    /// </summary>
    SetupObjectItem SetupObjectItem { get; }
}
