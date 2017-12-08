using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public partial class StObjCollectorResult
    {
        /// <summary>
        /// Generates final assembly.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="finalFilePath">Full path of the final dynamic assembly. Must end with '.dll'.</param>
        /// <param name="saveSource">Whether generated source files must be saved alongside the final dll.</param>
        /// <returns>False if any error occured (logged into <paramref name="monitor"/>).</returns>
        public CodeGenerateResult GenerateFinalAssembly( IActivityMonitor monitor, string finalFilePath, bool saveSource )
        {
            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
            using( monitor.OpenInfo( "Generating StObj dynamic assembly." ) )
            {
                var r = GenerateSourceCode( monitor, finalFilePath, true );
                Debug.Assert( r.Success || hasError, "!success ==> An error has been logged." );
                return r;
            }
        }
    }
}

