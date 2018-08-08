using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    public class AmbientServiceCollectorResult
    {
        internal AmbientServiceCollectorResult( IReadOnlyList<AmbientServiceInterfaceInfo> s, IReadOnlyList<AmbientServiceClassInfo> c, IReadOnlyList<AmbientServiceClassInfo> requiresUnification )
        {
            if( s != null ) Interfaces = s;
            else
            {
                Interfaces = Array.Empty<AmbientServiceInterfaceInfo>();
                HasFatalError = true;
            }
            if( c != null ) Classes = c;
            else
            {
                Classes = Array.Empty<AmbientServiceClassInfo>();
                HasFatalError = true;
            }
            UnificationRequired = requiresUnification ?? Array.Empty<AmbientServiceClassInfo>();
        }

        /// <summary>
        /// Gets the most specialized service interfaces found.
        /// Extended base interfaces are not exposed here.
        /// Use <see cref="AmbientServiceInterfaceInfo.Interfaces"/> to retrieve the base interfaces.
        /// </summary>
        public IReadOnlyList<AmbientServiceInterfaceInfo> Interfaces { get; }

        /// <summary>
        /// Gets the most specialized service implementations found.
        /// Base classes are not exposed here.
        /// Use <see cref="AmbientServiceClassInfo.Generalization"/>
        /// </summary>
        public IReadOnlyList<AmbientServiceClassInfo> Classes { get; }

        /// <summary>
        /// Gets a list of base classes that must be unified since they have at least
        /// two specializations.
        /// </summary>
        public IReadOnlyList<AmbientServiceClassInfo> UnificationRequired { get; }

        /// <summary>
        /// Gets whether an error exists that prevents the process to continue.
        /// </summary>
        /// <returns>
        /// False to continue the process (only warnings - or error considered as 
        /// warning - occured), true to stop remaining processes.
        /// </returns>
        public bool HasFatalError { get; private set; }

        /// <summary>
        /// Logs detailed information about discovered items.
        /// </summary>
        /// <param name="monitor">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityMonitor monitor )
        {

        }

    }
}
