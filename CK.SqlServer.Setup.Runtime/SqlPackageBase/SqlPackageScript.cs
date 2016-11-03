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
        internal SqlPackageScript( string key, ISqlServerParsedText script )
        {
            ScriptKey = key;
            Script = script;
        }

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
