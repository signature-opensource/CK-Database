#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\SetupCore\SetupEngineRegisterResult.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    class SetupCoreEngineRegisterResult
    {
        internal SetupCoreEngineRegisterResult( DependencySorterResult<ISetupItem> s )
        {
            SortResult = s;
        }

        /// <summary>
        /// Gets the <see cref="DependencySorterResult"/>. Null if an <see cref="UnexpectedError"/> occurred
        /// during its initialization or if a <see cref="CancelReason"/> has been set.
        /// </summary>
        public DependencySorterResult<ISetupItem> SortResult { get; private set; }

        /// <summary>
        /// Gets whether the <see cref="SetupCoreEngine.Register"/> succeeded: <see cref="SortResult"/>.<see cref="IDependencySorterResult.IsComplete">IsComplete</see>
        /// must be true, no <see cref="UnexpectedError"/> occurred, no <see cref="CancelReason"/> exists and no <see cref="CanceledRegistrationCulprit"/> canceled the registration.
        /// </summary>
        public bool IsValid
        {
            get 
            {
                Debug.Assert( SortResult != null || (UnexpectedError != null || CancelReason != null), "(SortResult == null) ==> (UnexpectedError != null || CancelReason != null)" );
                return UnexpectedError == null && CanceledRegistrationCulprit == null && CancelReason == null && SortResult.IsComplete; 
            }
        }

        /// <summary>
        /// Gets any <see cref="Exception"/> that may be thrown during registration.
        /// </summary>
        public Exception UnexpectedError { get; internal set; }

        /// <summary>
        /// Gets the cancellation reason if any.
        /// </summary>
        public string CancelReason { get; internal set; }

        /// <summary>
        /// Not null if a cancellation occured during registration by any <see cref="SetupCoreEngine.DriverEvent"/> listeners.
        /// Detailed error information should exist in log.
        /// </summary>
        public ISortedItem CanceledRegistrationCulprit { get; internal set; }

        /// <summary>
        /// Logs any error: <see cref="UnexpectedError"/> and any <see cref="DependencySorterResult"/> errors. 
        /// Does nothing if <see cref="IsValid"/> is true.
        /// </summary>
        /// <param name="_monitor">The _monitor to use.</param>
        public void LogError( IActivityMonitor monitor )
        {
            Debug.Assert( SortResult != null || (UnexpectedError != null || CancelReason != null), "(SortResult == null) ==> (UnexpectedError != null || CancelReason != null)" );
            if( monitor == null ) throw new ArgumentNullException( "_monitor" );
            if( !IsValid )
            {
                if( UnexpectedError != null ) monitor.Fatal().Send( UnexpectedError );
                if( CanceledRegistrationCulprit != null ) monitor.Fatal().Send( "Canceled during '{0}' registration.", CanceledRegistrationCulprit.FullName );
                if( CancelReason != null ) monitor.Fatal().Send( CancelReason );
                if( SortResult != null ) SortResult.LogError( monitor );
            }
        }

    }

}
