using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Text;
using System.Reflection;
using CK.Setup;

namespace CK.Core
{
    /// <summary>
    /// Result of the <see cref="AmbientTypeCollector"/> work.
    /// </summary>
    public class AmbientTypeCollectorResult
    {

        internal AmbientTypeCollectorResult(
            ISet<Assembly> assemblies,
            IPocoSupportResult pocoSupport,
            AmbientContractCollectorResult c,
            AmbientServiceCollectorResult s )
        {
            PocoSupport = pocoSupport;
            Assemblies = assemblies;
            AmbientContracts = c;
            AmbientServices = s;
        }

        /// <summary>
        /// Gets all the registered Poco information.
        /// </summary>
        public IPocoSupportResult PocoSupport { get; }

        /// <summary>
        /// Gets the set of asssemblies for which at least one type has been registered.
        /// </summary>
        public ISet<Assembly> Assemblies { get; }

        /// <summary>
        /// Gets the reults for <see cref="IAmbientContract"/> objects.
        /// </summary>
        public AmbientContractCollectorResult AmbientContracts { get; }

        /// <summary>
        /// Gets the reults for <see cref="IAmbientService"/> objects.
        /// </summary>
        public AmbientServiceCollectorResult AmbientServices { get; }

        /// <summary>
        /// Gets whether an error exists that prevents the process to continue.
        /// </summary>
        /// <returns>
        /// False to continue the process (only warnings - or error considered as 
        /// warning - occured), true to stop remaining processes.
        /// </returns>
        public bool HasFatalError => PocoSupport == null || AmbientContracts.HasFatalError || AmbientServices.HasFatalError;

        /// <summary>
        /// Logs detailed information about discovered items.
        /// </summary>
        /// <param name="monitor">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof(monitor) );
            using( monitor.OpenTrace( $"Collector summary:" ) )
            {
                if( PocoSupport == null )
                {
                    monitor.Fatal( $"Poco support failed!" );
                }
                AmbientContracts.LogErrorAndWarnings( monitor );
                AmbientServices.LogErrorAndWarnings( monitor );
            }
        }

    }

}
