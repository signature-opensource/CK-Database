using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class MultiAmbiantContractResult : MultiContextResult<AmbiantContractResult>
    {
        /// <summary>
        /// Logs detailed information about discovered ambiant contracts for all discovered contexts. Returns false if an error
        /// should prevent the process to continue (currently if a class or an interface ambiguity is found, false is returned).
        /// </summary>
        /// <param name="logger">Logger (must not be null).</param>
        /// <returns>True to continue the process (only warnings occured).</returns>
        public void LogErrorAndWarnings( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            using( logger.OpenGroup( LogLevel.Trace, "Ambiant Contract discovering: {0} context(s).", Count ) )
            {
                Foreach( r => r.LogErrorAndWarnings( logger ) );
            }
        }

    }
}
