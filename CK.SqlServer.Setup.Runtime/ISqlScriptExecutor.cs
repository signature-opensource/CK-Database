using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public interface ISqlScriptExecutor : IDisposable
    {
        /// <summary>
        /// Executes a single script (not a batch with GO separators).
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <returns>True on success.</returns>
        bool Execute( string script );        

    }

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
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
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
