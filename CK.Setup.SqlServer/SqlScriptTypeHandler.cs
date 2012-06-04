using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlScriptTypeHandler : IScriptTypeHandler
    {
        SqlManager _manager;
        IScriptExecutor _executor;
        string _scriptType;
        List<string> _requires;
        List<string> _requiredby;

        public SqlScriptTypeHandler( SqlManager manager )
        {
            if( manager == null ) throw new ArgumentNullException( "manager" );
            _manager = manager;
            _scriptType = "file-sql";
        }

        /// <summary>
        /// Gets or sets the script type.
        /// Defaults to "file-sql".
        /// </summary>
        public string ScriptType
        {
            get { return _scriptType; }
            set 
            {
                if( String.IsNullOrWhiteSpace( value ) ) throw new ArgumentNullException( "value" );
                _scriptType = value; 
            }
        }

        public IScriptExecutor CreateExecutor( IActivityLogger logger, SetupDriverContainer container )
        {
            return _executor ?? (_executor = new PackageSqlScriptExecutor( _manager, container.Engine.Memory ));
        }

        public void Release( IActivityLogger logger, IScriptExecutor executor )
        {
        }

        /// <summary>
        /// Gets script types that must be executed before this one. 
        /// Use '?' prefix to specify that the handler is not required (like "?res-sql").
        /// Can be null if no such handler exists.
        /// </summary>
        public IList<string> Requires
        {
            get { return _requires ?? (_requires = new List<string>()); }
        }

        /// <summary>
        /// Gets names of revert dependencies: scripts for this handler 
        /// will be executed before scripts for them. 
        /// Can be null if no such handler exists.
        /// </summary>
        public IList<string> RequiredBy
        {
            get { return _requiredby ?? (_requiredby = new List<string>()); }
        }

        IEnumerable<string> IScriptTypeHandler.Requires
        {
            get { return _requires; }
        }

        IEnumerable<string> IScriptTypeHandler.RequiredBy
        {
            get { return _requiredby; }
        }

    }
}
