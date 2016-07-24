#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\SqlMultiScript.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    class SqlMultiScript : MultiScriptBase
    {
        readonly ISetupSessionMemory _memory;
        readonly ISqlManager _manager;
        readonly SetupItemDriver _driver;
        List<SimpleScriptTagHandler.Script> _scripts;
        ISqlScriptExecutor _executor;

        public SqlMultiScript( IActivityMonitor monitor, ISetupScript script, ISqlManager manager, ISetupSessionMemory memory, SetupItemDriver driver )
            : base( monitor, script )
        {
            if( memory == null ) throw new ArgumentNullException( nameof( memory ) );
            if( manager == null ) throw new ArgumentNullException( nameof( manager) );
            if( driver == null ) throw new ArgumentNullException( nameof( driver ) );
            _manager = manager;
            _memory = memory;
            _driver = driver;
        }

        public override bool ExecuteScript()
        {
            if( _memory.IsItemRegistered( Script.GetScriptKey() ) ) return true;
            using( _executor = _manager.CreateExecutor( Monitor ) )
            {
                return base.ExecuteScript();
            }
        }

        protected override IReadOnlyList<string> SplitScripts( string scriptBody )
        {
            var s = new SimpleScriptTagHandler( scriptBody );
            if( !s.Expand( Monitor, true ) ) return null;
            _scripts = s.SplitScript();
            return _scripts.Select( script => script.Body ).ToArray();
        }

        protected override bool ExecuteOneScript( int numScript, string scriptBody )
        {
            if( _memory.IsItemRegistered( GetScriptKey( numScript ) ) ) return true;
            if( Script.ScriptSource.EndsWith( "-y4" ) )
            {
                scriptBody = SqlPackageBaseItem.ProcessY4Template( Monitor, _driver, _driver.Item, null, Script.Name.FileName, scriptBody );
            }
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

}
