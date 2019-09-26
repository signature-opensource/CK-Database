#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\SetupCore\ISetupSessionMemoryProvider.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup
{
    /// <summary>
    /// Provides memory for the setup process.
    /// </summary>
    public interface ISetupSessionMemoryProvider
    {
        /// <summary>
        /// Gets the date and time of the previous start. <see cref="DateTime.MinValue"/> when
        /// there is no previous setup.
        /// </summary>
        DateTime LastStartDate { get; }

        /// <summary>
        /// Gets the number of non terminated setup attempts.
        /// </summary>
        int StartCount { get; }

        /// <summary>
        /// Gets a description of the last error (set by <see cref="StopSetup"/>).
        /// </summary>
        string LastError { get; }

        /// <summary>
        /// Gets whether <see cref="StartSetup"/> has been called and <see cref="StopSetup"/> has 
        /// not yet been called.
        /// </summary>
        bool IsStarted { get; }
        
        /// <summary>
        /// Starts a setup session. <see cref="IsStarted"/> must be false 
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <returns>A persistent memory that will be used by the setup process.</returns>
        ISetupSessionMemory StartSetup();

        /// <summary>
        /// On success, the whole memory of the setup process must be cleared. 
        /// On error (when <paramref name="error"/> is not null), the memory must be persisted.
        /// <see cref="IsStarted"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="error">
        /// Must be not null to indicate an error. Null on success. 
        /// Empty or white space will raise an <see cref="ArgumentException"/>.
        /// </param>
        void StopSetup( string error );

    }
}
