using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Event argument for <see cref="SetupEngine.SetupEvent"/>.
    /// </summary>
    public class SetupEventArgs : EventArgs
    {
        internal string CancelReason;

        /// <summary>
        /// Gets the current step. Can be None (before registration), Init, Install, Settle and Done.
        /// </summary>
        public SetupStep Step { get; private set; }
        
        /// <summary>
        /// Gets whether an error occured during <see cref="Step"/>.
        /// </summary>
        public bool ErrorOccurred { get; private set; }

        /// <summary>
        /// Enables any receiver of this event to stop the setup process. A reason is required (not null nor empty). 
        /// </summary>
        public void CancelSetup( string cancelReason )
        {
            if( String.IsNullOrWhiteSpace( cancelReason ) ) throw new ArgumentException( "cancelReason" );
            CancelReason = cancelReason;
        }

        internal SetupEventArgs( SetupStep step, bool errorOccured = false )
        {
            Step = step;
            ErrorOccurred = errorOccured;
        }

    }
}
