using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using CK.Reflection;
using System.Resources;
using System.Collections;
using System.IO;

namespace CK.Setup
{
    public partial class StObjCollectorResult
    {
        /// <summary>
        /// Generates final assembly.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>False if any error occured (logged into <paramref name="monitor"/>).</returns>
        public bool GenerateFinalAssembly( IActivityMonitor monitor )
        {
            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
            using( monitor.OpenInfo( "Generating StObj dynamic assembly." ) )
            {
                try
                {
                    bool success = GenerateSourceCode( monitor, true );
                    Debug.Assert( success || hasError, "!success ==> An error has been logged." );
                    return success;
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While generating final assembly '{_tempAssembly.SaveFileName}'.", ex );
                    return false;
                }
            }
        }
    }
}

