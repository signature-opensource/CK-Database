using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class AmbiantContractCollectorResult : MultiContextualResult<AmbiantContractCollectorContextualResult>
    {
        readonly AmbiantTypeMapper _mappings;

        internal AmbiantContractCollectorResult( AmbiantTypeMapper mappings )
        {
            _mappings = mappings;
        }

        /// <summary>
        /// Logs detailed information about discovered ambiant contracts for all discovered contexts.
        /// </summary>
        /// <param name="logger">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            using( logger.OpenGroup( LogLevel.Trace, "Ambiant Contract discovering: {0} context(s).", Count ) )
            {
                Foreach( r => r.LogErrorAndWarnings( logger ) );
            }
        }

        /// <summary>
        /// Gets the type mapper for the multiple existing contexts.
        /// </summary>
        public IAmbiantTypeMapper Mappings
        {
            get { return _mappings; }
        }

    }
}
