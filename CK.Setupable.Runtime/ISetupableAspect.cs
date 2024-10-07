using System;

namespace CK.Setup;

/// <summary>
/// Setupable aspect contract.
/// </summary>
public interface ISetupableAspect
{
    /// <summary>
    /// Triggered before registration.
    /// This event fires before the <see cref="SetupEvent"/> with <see cref="SetupEventArgs.Step"/> set to None,
    /// and enables registration of setup items.
    /// </summary>
    event EventHandler<RegisterSetupEventArgs> RegisterSetupEvent;

    /// <summary>
    /// Triggered for each steps of <see cref="SetupStep"/>:
    /// None (before registration), Init, Install, Settle and Done.
    /// </summary>
    event EventHandler<SetupEventArgs> SetupEvent;

    /// <summary>
    /// Triggered for each <see cref="DriverBase"/> setup phases.
    /// </summary>
    event EventHandler<DriverEventArgs> DriverEvent;


}
