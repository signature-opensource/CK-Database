using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Event argument for <see cref="SetupCenter.DriverEvent"/>.
    /// </summary>
    public class SetupDriverEventArgs : EventArgs
    {
        /// <summary>
        /// The current step. <see cref="SetupStep.None"/> is used 
        /// during <see cref="SetupCenter.RegisterItemsOrDiscoverers"/>.
        /// </summary>
        public SetupStep Step { get; private set; }
        
        /// <summary>
        /// The <see cref="SetupDriverBase"/> that has been registered, initialized,
        /// installed or setlled.
        /// </summary>
        public SetupDriverBase Driver { get; internal set; }

        internal SetupDriverEventArgs( SetupStep step )
        {
            Step = step;
        }

    }
}
