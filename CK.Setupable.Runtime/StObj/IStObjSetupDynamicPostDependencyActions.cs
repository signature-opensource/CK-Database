using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Defines a state available during dynamic initialization of <see cref="IMutableSetupItem"/> for <see cref="IStObjResult"/>.
    /// <see cref="IStObjSetupDynamicInitializer.DynamicItemInitialize"/> methods are called according to dependency order:
    /// this interface enables DynamicItemInitialize methods to <see cref="PushAction"/> that will be executed once dependent objects are initialized and
    /// offers a persistent <see cref="Memory"/> that can be used to share information between the participants.
    /// </summary>
    public interface IStObjSetupDynamicInitializerState
    {
        /// <summary>
        /// Gets the activity logger to use.
        /// </summary>
        IActivityLogger Logger { get; }

        /// <summary>
        /// Gets an associative dictionary that memorizes states between actions.
        /// </summary>
        IDictionary Memory { get; }

        /// <summary>
        /// Pushes an action that will be executed after the dynamic initialization of the dependent objects.
        /// An action can be pushed at any moment: a pushed action can push another action.
        /// </summary>
        /// <param name="postAction">Action to execute.</param>
        void PushAction( Action<IStObjSetupDynamicInitializerState, IMutableSetupItem, IStObjResult> postAction );
    }
}
