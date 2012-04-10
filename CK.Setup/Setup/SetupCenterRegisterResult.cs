using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class SetupCenterRegisterResult
    {
        internal SetupCenterRegisterResult( DependencySorterResult s )
        {
            SortResult = s;
        }

        /// <summary>
        /// Gets the <see cref="DependencySorterResult"/>. Null if an <see cref="UnexpectedError"/> occurred
        /// during its initialization.
        /// </summary>
        public DependencySorterResult SortResult { get; private set; }

        /// <summary>
        /// Gets whether the <see cref="SetupCenter.RegisterItemsOrDiscoverers"/> succeeded: <see cref="SortResult"/>.<see cref="DependencySorterResult.IsComplete">IsComplete</see>
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

    }

}
