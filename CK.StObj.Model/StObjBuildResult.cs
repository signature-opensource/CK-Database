using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates the result of the <see cref="StObjContextRoot.Build"/> or <see cref="StObjContextRoot.LoadOrBuild"/> methods.
    /// </summary>
    public class StObjBuildResult : IDisposable
    {
        readonly IActivityLogger _logger;

        internal StObjBuildResult( bool success, AppDomain d, IActivityLogger loggerForAppDomainUnloadError )
        {
            Success = success;
            IndependentAppDomain = d;
            _logger = loggerForAppDomainUnloadError;
        }

        /// <summary>
        /// Gets wether the build succeeded.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Gets the independent Application Domain that has been used.
        /// It is not null if and only if <see cref="BuilderAppDomainConfiguration.UseIndependentAppDomain"/> is true and 
        /// this <see cref="StObjBuildResult"/> has not been disposed.
        /// </summary>
        public AppDomain IndependentAppDomain { get; private set; }

        /// <summary>
        /// Gets the final <see cref="IStObjMap"/> to use. This is available (not null) 
        /// if and only if <see cref="Success"/> is true and <see cref="StObjContextRoot.LoadOrBuild"/> has been called.
        /// </summary>
        public IStObjMap StObjMap { get; internal set; }

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
                }
                catch( Exception ex )
                {
                    _logger.Error( ex, "While unloading independent AppDomain." );
                }
                IndependentAppDomain = null;
            }
        }
    }
}
