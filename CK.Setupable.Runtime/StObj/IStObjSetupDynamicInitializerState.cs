using System;
using System.Collections;
using CK.Core;

namespace CK.Setup;

/// <summary>
/// Defines a state available during dynamic initialization of <see cref="IMutableSetupItem"/> for <see cref="IStObjResult"/>.
/// <see cref="IStObjSetupDynamicInitializer.DynamicItemInitialize"/> methods are called according to dependency order:
/// this interface enables DynamicItemInitialize methods to <see cref="PushAction"/> that will be executed once dependent objects are initialized and
/// offers a persistent <see cref="Memory"/> that can be used to share information between the participants.
/// Multiple rounds of initializations are supported thanks to <see cref="PushNextRoundAction"/>.
/// </summary>
public interface IStObjSetupDynamicInitializerState
{
    /// <summary>
    /// Gets the activity monitor to use.
    /// </summary>
    IActivityMonitor Monitor { get; }

    /// <summary>
    /// Gets the services provider.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets an associative dictionary that memorizes states between actions.
    /// </summary>
    IDictionary Memory { get; }

    /// <summary>
    /// Gets the current round number. The very first round number is 0.
    /// As longs as <see cref="PushNextRoundAction"/> is called in a round, a new round is created
    /// (and will be executed).
    /// </summary>
    int CurrentRoundNumber { get; }

    /// <summary>
    /// Pushes an action that will be executed after the dynamic initialization of the dependent objects.
    /// An action can be pushed at any moment: a pushed action can push another action.
    /// </summary>
    /// <param name="postAction">Action to execute.</param>
    void PushAction( Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> postAction );

    /// <summary>
    /// Pushes an action that will be executed after all actions for the currently executing round.
    /// </summary>
    /// <remarks>
    /// There is no limit to the number of rounds: this mechanism has been prefered to a DynamicItemInitialize method
    /// (either on the <see cref="IStObjSetupDynamicInitializer"/> interface or on a new interface) precisely because
    /// there is no limit. Since "transformers" and other low level operations are handled by clever developpers ;), 
    /// this trampoline pattern offers efficient, easy and extensible way to implement complex, multi-phases processes.
    /// </remarks>
    /// <param name="a">The defferred action to execute during the next round.</param>
    void PushNextRoundAction( Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> a );
}
