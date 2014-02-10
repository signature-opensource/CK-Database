using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates the result of the <see cref="StObjContextRoot.Build"/> method.
    /// Must be <see cref="Dispose"/>d once done.
    /// </summary>
    public class StObjBuildResult : IDisposable
    {
        readonly IActivityMonitor _monitor;

        internal StObjBuildResult( bool success, string externalVersionStamp, bool assemblyAlreadyExists, AppDomain d, IActivityMonitor loggerForAppDomainUnloadError )
        {
            Success = success;
            ExternalVersionStamp = externalVersionStamp;
            AssemblyAlreadyExists = assemblyAlreadyExists;
            IndependentAppDomain = d;
            _monitor = loggerForAppDomainUnloadError;
        }

        /// <summary>
        /// Gets whether the build succeeded or the assembly with the same <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/> already exists.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Gets whether the assembly with the same <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/> has been found.
        /// </summary>
        public bool AssemblyAlreadyExists { get; private set; }

        /// <summary>
        /// Gets the version stamp of the built or already existing assembly.
        /// When a build has been done, it is the same as the input <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/>.
        /// </summary>
        public string ExternalVersionStamp { get; private set; }

        /// <summary>
        /// Gets the independent Application Domain that has been used to get the version stamp and/or build the assembly.
        /// It is null if this <see cref="StObjBuildResult"/> has been disposed or if no independent domain was needed.
        /// </summary>
        public AppDomain IndependentAppDomain { get; private set; }

        /// <summary>
        /// Unloads the <see cref="IndependentAppDomain"/> if it exists.
        /// </summary>
        public void Dispose()
        {
            if( IndependentAppDomain != null )
            {
                try
                {
                    AppDomain.Unload( IndependentAppDomain );
                    _monitor.Info().Send( "Independent AppDomain successfuly unloaded." );
                }
                catch( Exception ex )
                {
                    _monitor.Error().Send( ex, "While unloading independent AppDomain." );
                }
                IndependentAppDomain = null;
            }
        }
    }
}
