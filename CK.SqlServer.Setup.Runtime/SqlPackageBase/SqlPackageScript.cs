using CK.Setup;
using CK.SqlServer.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    public class SqlPackageScript
    {
        readonly SqlPackageBaseItemDriver _driver;
        readonly SetupCallGroupStep _step;

        internal SqlPackageScript( SqlPackageBaseItemDriver driver, SetupCallGroupStep step, string key, ISqlServerParsedText script )
        {
            _driver = driver;
            _step = step;
            ScriptKey = key;
            Script = script;
        }

        /// <summary>
        /// Gets the driver.
        /// </summary>
        public SqlPackageBaseItemDriver Driver => _driver;

        /// <summary>
        /// Gets the step for this script.
        /// </summary>
        public SetupCallGroupStep Step => _step;

        /// <summary>
        /// Gets a unique key that identifies this script.
        /// </summary>
        public string ScriptKey { get; }

        /// <summary>
        /// Gets the script itselF.
        /// </summary>
        public ISqlServerParsedText Script { get; }
    }
}
