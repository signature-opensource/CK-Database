using CK.CodeGen;
using CK.CodeGen.Abstractions;
using CK.Core;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
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
        /// <param name="informationalVersion">
        /// Informational version.
        /// <see cref="AssemblyInformationalVersionAttribute"/> is used when not null or empty.
        /// </param>
        /// <param name="skipCompilation">
        /// When true, compilation is skipped (but actual code generation step is always called).
        /// </param>
        /// <returns>False if any error occured (logged into <paramref name="monitor"/>).</returns>
        public CodeGenerateResult GenerateFinalAssembly(
            IActivityMonitor monitor,
            string finalFilePath,
            bool saveSource,
            string informationalVersion,
            bool skipCompilation = false )
        {
            bool hasError = false;
            using( monitor.OnError( () => hasError = true ) )
            using( monitor.OpenInfo( "Generating StObj dynamic assembly." ) )
            {
                if( !string.IsNullOrWhiteSpace( informationalVersion ) )
                {
                    _tempAssembly.DefaultGenerationNamespace.Workspace.Global
                            .Append( "[assembly:System.Reflection.AssemblyInformationalVersion(" )
                            .AppendSourceString( informationalVersion )
                            .Append( ")]" )
                            .NewLine();
                }
                var r = GenerateSourceCode( monitor, finalFilePath, saveSource, skipCompilation );
                Debug.Assert( r.Success || hasError, "!success ==> An error has been logged." );
                return r;
            }
        }
    }
}

