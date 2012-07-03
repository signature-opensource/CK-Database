using System;
using System.Diagnostics;
using CK.Core;
using System.Collections.Generic;

namespace CK.Setup
{
    public class PackageScriptHandler : PackageHandler
    {
        readonly PackageScriptSet _scripts;
        readonly IReadOnlyList<IScriptTypeHandler> _scriptHandlers;

        public PackageScriptHandler( PackageDriver driver, PackageScriptSet scripts, IReadOnlyList<IScriptTypeHandler> scriptHandlers )
            : base( driver )
        {
            if( scripts == null ) throw new ArgumentNullException( "scripts" );
            if( scriptHandlers == null ) throw new ArgumentNullException( "scriptHandlers" );
            if( scriptHandlers.Count == 0 ) throw new ArgumentException( "No script handlers provided.", "scriptHandlers" );
            _scripts = scripts;
            _scriptHandlers = scriptHandlers;
        }

        class ScriptLine
        {
            public readonly ContainerDriver Container;
            public readonly IScriptTypeHandler Handler;
            public readonly PackageTypedScriptVector Vector;

            IScriptExecutor _executor;

            public ScriptLine( ContainerDriver container, IScriptTypeHandler h, PackageTypedScriptVector v )
            {
                Container = container;
                Handler = h;
                Vector = v;
            }

            public IScriptExecutor GetExecutor( IActivityLogger logger )
            {
                if( _executor == null )
                {
                    _executor = Handler.CreateExecutor( logger, Container );
                    if( _executor == null )
                    {
                        logger.Error( "Unable to obtain a Script Executor for '{0}'.", Handler.ScriptType );
                    }
                }
                return _executor; 
            }

            internal void ReleaseExecutor( IActivityLogger logger )
            {
                if( _executor != null ) Handler.Release( logger, _executor );
            }
        }

        class StepScripts : IDisposable
        {
            List<ScriptLine> _lines;
            IActivityLogger _logger;

            public StepScripts( IActivityLogger logger, List<ScriptLine> lines )
            {
                _logger = logger;
                _lines = lines;
            }

            internal bool Execute( int totalScriptCount )
            {
                if( _lines.Count == 1 )
                {
                    ScriptLine line = _lines[0];
                    using( line.Vector.Scripts.Count > 1 ? _logger.OpenGroup( LogLevel.Info, "Executing {1} '{0}' scripts.", line.Handler.ScriptType, line.Vector.Scripts.Count ) : null )
                    {
                        IScriptExecutor e = line.GetExecutor( _logger );
                        if( e == null ) return false;
                        foreach( CoveringScript script in line.Vector.Scripts )
                        {
                            if( !e.ExecuteScript( _logger, script.Script ) )
                            {
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert( totalScriptCount > 1 );
                    using( _logger.OpenGroup( LogLevel.Info, "Executing {0} scripts ({1} different types).", totalScriptCount, _lines.Count ) )
                    {
                        throw new NotImplementedException( "Multiple Script type handling is not yet implemented." );
                    }
                }
                return true;
            }

            public void Dispose()
            {
                foreach( ScriptLine l in _lines ) l.ReleaseExecutor( _logger );
            }
        }

        bool Execute( SetupCallContainerStep step )
        {
            int totalScriptCount = 0;
            var lines = new List<ScriptLine>();
            foreach( IScriptTypeHandler h in _scriptHandlers )
            {
                 PackageTypedScriptVector v = _scripts.GetScriptVector( h.ScriptType, step, Driver.ExternalVersion != null ? Driver.ExternalVersion.Version : null, Driver.Item.Version );
                if( v.Scripts.Count > 0 )
                {
                    totalScriptCount += v.Scripts.Count;
                    lines.Add( new ScriptLine( Driver, h, v ) );
                }
            }
            if( lines.Count == 0 ) return true;
            using( StepScripts allScripts = new StepScripts( Driver.Engine.Logger, lines ) )
            {
                return allScripts.Execute( totalScriptCount );
            }
        }

        protected override bool Init()
        {
            return Execute( SetupCallContainerStep.Init );
        }

        protected override bool InitContent()
        {
            return Execute( SetupCallContainerStep.InitContent );
        }

        protected override bool Install()
        {
            return Execute( SetupCallContainerStep.Install );
        }

        protected override bool InstallContent()
        {
            return Execute( SetupCallContainerStep.InstallContent );
        }

        protected override bool Settle()
        {
            return Execute( SetupCallContainerStep.Settle );
        }

        protected override bool SettleContent()
        {
            return Execute( SetupCallContainerStep.SettleContent );
        }

    }
}
