using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Contains a set of scripts associated to a package.
    /// </summary>
    public class SqlPackageScripts
    {
        readonly SqlPackageBaseItem _package;
        readonly string[] _scripts;

        internal SqlPackageScripts( SqlPackageBaseItem package )
        {
            _package = package;
            Debug.Assert( (int)SetupCallGroupStep.Init == 1 && (int)SetupCallGroupStep.SettleContent == 6 );
            _scripts = new string[6];
        }

        /// <summary>
        /// Gets the script for the given <see cref="SetupCallGroupStep"/>. Null if none.
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public string this[ SetupCallGroupStep step ] => _scripts[(int)step - 1];

        public void Set( IActivityMonitor monitor, SetupCallGroupStep step, string script, string sourceName )
        {

        }
    }
}
