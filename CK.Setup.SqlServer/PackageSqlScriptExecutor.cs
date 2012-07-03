using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.SqlServer;
using CK.Core;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    class SqlMultiScript : MultiScriptBase
    {
        ISetupSessionMemory _memory;
        List<SimpleScriptTagHandler.Script> _scripts;
        ISqlScriptExecutor _executor;
        SqlManager _manager;

        public SqlMultiScript( IActivityLogger logger, ISetupScript script, SqlManager manager, ISetupSessionMemory memory )
            : base( logger, script )
        {
            if( memory == null ) throw new ArgumentNullException( "memory" );
            if( manager == null ) throw new ArgumentNullException( "manager" );
            _manager = manager;
            _memory = memory;
        }

        public override bool ExecuteScript()
        {
            if( _memory.IsItemRegistered( Script.GetScriptKey() ) ) return true;
            using( _executor = _manager.CreateExecutor( Logger ) )
            {
                return base.ExecuteScript();
            }
        }

        protected override IReadOnlyList<string> SplitScripts( string scriptBody )
        {
            var s = new SimpleScriptTagHandler( scriptBody );
            if( !s.Expand( Logger, true ) ) return null;
            _scripts = s.SplitScript();
            return _scripts.Select( script => script.Body ).ToReadOnlyList();
        }

        protected override bool ExecuteOneScript( int numScript, string scriptBody )
        {
            if( _memory.IsItemRegistered( GetScriptKey( numScript ) ) ) return true;
            return _executor.Execute( scriptBody );
        }

        protected override void OnOneScriptSucceed( int numScript, string scriptBody )
        {
            _memory.RegisterItem( GetScriptKey( numScript ) );
        }

        protected override void OnScriptSucceed()
        {
            _memory.RegisterItem( Script.GetScriptKey() );
        }
        
        string GetScriptKey( int numScript )
        {
            SimpleScriptTagHandler.Script s = _scripts[numScript];
            string label = s.Label ?? String.Format( "AutoLabel{0}", numScript );
            return Script.GetScriptKey( label );
        }

    }

    class PackageSqlScriptExecutor : MultiScriptExecutorBase
    {
        SqlManager _manager;
        ISetupSessionMemory _memory;

        public PackageSqlScriptExecutor( SqlManager m, ISetupSessionMemory memory )
        {
            Debug.Assert( m != null );
            _manager = m;
            _memory = memory;
        }

        protected override MultiScriptBase CreateMultiScript( IActivityLogger logger, ISetupScript script )
        {
            return new SqlMultiScript( logger, script, _manager, _memory );
        }

    }
}
