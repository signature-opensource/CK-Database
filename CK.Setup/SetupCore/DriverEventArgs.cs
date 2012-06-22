using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Event argument for <see cref="SetupEngine.DriverEvent"/>.
    /// </summary>
    public class DriverEventArgs : EventArgs
    {
        /// <summary>
        /// The current step. <see cref="SetupStep.None"/> is used 
        /// during <see cref="SetupEngine.Register"/>.
        /// </summary>
        public SetupStep Step { get; private set; }
        
        /// <summary>
        /// The <see cref="DriverBase"/> that has been registered, initialized,
        /// installed or setlled.
        /// </summary>
        public DriverBase Driver { get; internal set; }

        /// <summary>
        /// Gets or sets a flag to stop the setup process. 
        /// This should be set to true after at least one fatal error has been 
        /// logged with a detailed explanation. 
        /// </summary>
        public bool CancelSetup { get; set; }

        internal DriverEventArgs( SetupStep step )
        {
            Step = step;
        }

    }
}
