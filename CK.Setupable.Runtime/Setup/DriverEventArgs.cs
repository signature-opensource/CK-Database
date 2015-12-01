#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\DriverEventArgs.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Event argument for the <see cref="ISetupEngine.DriverEvent"/>.
    /// </summary>
    public class DriverEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current step. See remarks.
        /// </summary>
        /// <remarks>
        /// the step <see cref="SetupStep.PreInit"/> is used during registration (right after <see cref="DriverBase"/> object 
        /// instanciation: its dependencies' drivers are available but not the whole set of drivers).
        /// </remarks>
        public SetupStep Step { get; private set; }
        
        /// <summary>
        /// The <see cref="DriverBase"/> that has been registered, initialized,
        /// installed or settled.
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
