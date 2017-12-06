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
        /// Generates final assembly must be called only when <see cref="BuilderFinalAssemblyConfiguration.GenerateFinalAssemblyOption"/>
        /// is not <see cref="BuilderFinalAssemblyConfiguration.GenerateOption.DoNotGenerateFile"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="callPEVrify">True to call PEVerify on the generated assembly.</param>
        /// <returns>False if any error occured (logged into <paramref name="monitor"/>).</returns>
        public bool GenerateFinalAssembly( IActivityMonitor monitor, bool callPEVrify, bool withIL, bool withSrc )
        {
            using( monitor.OpenInfo( "Generating StObj dynamic assembly." ) )
            {
                try
                {
                    return withSrc ? GenerateSourceCode( monitor, true, withIL ) : true;
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

