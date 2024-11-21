namespace CK.Setup;

/// <summary>
/// Extends <see cref="IStObjSetupDynamicInitializerState"/>.
/// </summary>
public static class StObjSetupDynamicInitializerStateExtension
{
    /// <summary>
    /// Finds a dynamic resource by its full name.
    /// </summary>
    /// <param name="this">This state.</param>
    /// <param name="fullName">The name of the setup item to find.</param>
    /// <returns>The setup item wrapper or null.</returns>
    public static SetupObjectItemDynamicResource FindItem( this IStObjSetupDynamicInitializerState @this, string fullName )
    {
        return (SetupObjectItemDynamicResource)@this.Memory[new SetupObjectItemDynamicResource( fullName )];
    }
}
