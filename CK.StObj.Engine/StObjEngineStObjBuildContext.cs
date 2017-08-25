using System;
using System.Collections.Generic;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    class StObjEngineStObjBuildContext : IStObjEngineStObjBuildContext
    {
        readonly IActivityMonitor _monitor;
        readonly StObjEngineConfigurationContext _startContext;
        readonly IReadOnlyList<IStObjResult> _orderedStObjs;
        readonly List<Func<IActivityMonitor, IStObjEngineStObjBuildContext, bool>> _postActions;

        public StObjEngineStObjBuildContext( IActivityMonitor monitor, StObjEngineConfigurationContext startContext, IReadOnlyList<IStObjResult> stObjs )
        {
            _monitor = monitor;
            _startContext = startContext;
            _postActions = new List<Func<IActivityMonitor, IStObjEngineStObjBuildContext, bool>>();
            _orderedStObjs = stObjs;
        }

        public IServiceProvider Services => _startContext.ServiceContainer;

        public IReadOnlyList<IStObjEngineAspect> Aspects => _startContext.Aspects;

        public IReadOnlyList<IStObjResult> OrderedStObjs => _orderedStObjs;

        public void PushDeferredAction( Func<IActivityMonitor, IStObjEngineStObjBuildContext, bool> postAction )
        {
            if( postAction == null ) throw new ArgumentNullException( nameof( postAction ) );
            _postActions.Add( postAction );
        }

        bool ExecutePostActions()
        {
            int i = 0;
            while( i < _postActions.Count )
            {
                var a = _postActions[i];
                _postActions[i++] = null;
                if( !a( _monitor, this ) )
                {
                    _monitor.Error( "A defered OnStObjBuild action failed." );
                    return false;
                }
            }
            _postActions.Clear();
            return true;
        }

        internal bool RunOnStObjBuild()
        {
            using( _monitor.OpenInfo( "Running aspects' OnStObjBuild." ) )
            {
                foreach( var a in _startContext.Aspects )
                {
                    using( _monitor.OpenInfo( $"Aspect: {a.GetType().FullName}." ) )
                    {
                        try
                        {
                            if( !a.OnStObjBuild( _monitor, this ) ) return false;
                        }
                        catch( Exception ex )
                        {
                            _monitor.Error( ex );
                            return false;
                        }
                    }
                }
                return ExecutePostActions();
            }
        }
    }
}
