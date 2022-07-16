#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\ISqlScriptExecutor.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.SqlServer
{
    /// <summary>
    /// Basic script executor. It is a disposable object.
    /// </summary>
    public interface ISqlScriptExecutor : IDisposable
    {
        /// <summary>
        /// Executes a single script (not a batch with GO separators).
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <returns>True on success.</returns>
        bool Execute( string script );        

    }

    /// <summary>
    /// Extends <see cref="ISqlScriptExecutor"/> to support multiple scripts execution at once.
    /// </summary>
    public static class SqlScriptExecutorExtension
    {
        /// <summary>
        /// Executes multiple scripts.
        /// </summary>
        /// <param name="this">This <see cref="ISqlScriptExecutor"/>.</param>
        /// <param name="scripts">A set of scripts.</param>
        /// <param name="stopOnError">False to continue execution regardless of a script failure.</param>
        /// <returns>The number of script that failed.</returns>
        public static int Execute( this ISqlScriptExecutor @this, IEnumerable<string> scripts, bool stopOnError = true )
        {
            Throw.CheckNotNullArgument( scripts );
            int failCount = 0;
            foreach( string s in scripts )
            {
                if( s != null && !@this.Execute( s ) )
                {
                    ++failCount;   
                    if( !stopOnError ) break;
                }
            }
            return failCount;
        }
    }

}
