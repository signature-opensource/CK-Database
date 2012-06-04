using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public class SetupEngineRegisterResult
    {
        internal SetupEngineRegisterResult( DependencySorterResult s )
        {
            SortResult = s;
        }

        /// <summary>
        /// Gets the <see cref="DependencySorterResult"/>. Null if an <see cref="UnexpectedError"/> occurred
        /// during its initialization.
        /// </summary>
        public DependencySorterResult SortResult { get; private set; }

        /// <summary>
        /// Gets whether the <see cref="SetupEngine.Register"/> succeeded: <see cref="SortResult"/>.<see cref="DependencySorterResult.IsComplete">IsComplete</see>
        /// must be true and no <see cref="UnexpectedError"/> occurred.
        /// </summary>
        public bool IsValid
        {
            get { return SortResult.IsComplete && UnexpectedError == null; }
        }

        /// <summary>
        /// Gets any <see cref="Exception"/> that may be thrown during registration.
        /// </summary>
        public Exception UnexpectedError { get; internal set; }

        /// <summary>
        /// Not null if a cancellation occured during registration by any <see cref="SetupEngine.DriverEvent"/> listeners.
        /// Detailed error information should exist in log.
        /// </summary>
        public ISortedItem CanceledRegistrationCulprit { get; internal set; }

        /// <summary>
        /// Logs any error: <see cref="UnexpectedError"/> and any <see cref="DependencySorterResult"/> errors. 
        /// Does nothing if <see cref="IsValid"/> is true.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        public void LogError( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( !IsValid )
            {
                if( UnexpectedError != null ) logger.Error( UnexpectedError );
                if( CanceledRegistrationCulprit != null ) logger.Error( "Canceled during '{0}' registration.", CanceledRegistrationCulprit.FullName );
                SortResult.LogError( logger );
            }
        }

    }

}
