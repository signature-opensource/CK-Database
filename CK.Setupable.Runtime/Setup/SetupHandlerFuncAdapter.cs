using CK.Core;
using System;

namespace CK.Setup;

/// <summary>
/// Raw <see cref="ISetupHandler"/> that relays the call to a function.
/// This handler is not bound to a driver but to a specific <see cref="SetupStep"/>.
/// </summary>
public class SetupHandlerFuncAdapter : ISetupHandler
{
    readonly Func<IActivityMonitor, SetupItemDriver, bool> _func;
    readonly SetupCallGroupStep _step;

    /// <summary>
    /// Initializes a new <see cref="SetupHandlerFuncAdapter"/> with a function for a specific step.
    /// </summary>
    /// <param name="handler">The function to call.</param>
    /// <param name="step">The step at which the handler must be called.</param>
    public SetupHandlerFuncAdapter( Func<IActivityMonitor, SetupItemDriver, bool> handler, SetupCallGroupStep step )
    {
        _func = handler;
        _step = step;
    }

    bool ISetupHandler.OnStep( IActivityMonitor monitor, SetupItemDriver d, SetupCallGroupStep step )
    {
        return _step == step ? _func( monitor, d ) : true;
    }

    bool ISetupHandler.Init( IActivityMonitor monitor, SetupItemDriver d ) => true;

    bool ISetupHandler.InitContent( IActivityMonitor monitor, SetupItemDriver d ) => true;

    bool ISetupHandler.Install( IActivityMonitor monitor, SetupItemDriver d ) => true;

    bool ISetupHandler.InstallContent( IActivityMonitor monitor, SetupItemDriver d ) => true;

    bool ISetupHandler.Settle( IActivityMonitor monitor, SetupItemDriver d ) => true;

    bool ISetupHandler.SettleContent( IActivityMonitor monitor, SetupItemDriver d ) => true;

}
