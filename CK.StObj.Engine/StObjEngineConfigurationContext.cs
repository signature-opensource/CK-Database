using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.Setup
{
    sealed class StObjEngineConfigurationContext : IStObjEngineConfigurationContext
    {
        sealed class Container : SimpleServiceContainer
        {
            readonly StObjEngineConfigurationContext _c;

            public Container( StObjEngineConfigurationContext c )
            {
                _c = c;
            }

            protected override object GetDirectService( Type serviceType )
            {
                object s = base.GetDirectService( serviceType );
                if( s == null )
                {
                    if( serviceType == typeof(IActivityMonitor)
                        || serviceType == typeof(ActivityMonitor) )
                    {
                        s = _c._monitor;
                    }
                }
                return s;
            }
        }

        readonly IActivityMonitor _monitor;
        readonly List<IStObjEngineAspect> _aspects;
        readonly List<Func<IActivityMonitor, IStObjEngineConfigurationContext, bool>> _postActions;
        readonly Container _container;
        readonly SimpleServiceContainer _configureOnlycontainer;
        readonly StObjEngineConfigurator _configurator;

        List<Type> _explicitRegisteredClasses;
        Action<IEnumerable<IDependentItem>> _stObjDependencySorterHookInput;
        Action<IEnumerable<ISortedItem>> _stObjDependencySorterHookOutput;

        internal StObjEngineConfigurationContext( IActivityMonitor monitor )
        {
            _monitor = monitor;
            _aspects = new List<IStObjEngineAspect>();
            _postActions = new List<Func<IActivityMonitor, IStObjEngineConfigurationContext, bool>>();
            _container = new Container( this );
            _configurator = new StObjEngineConfigurator();
            _configureOnlycontainer = new SimpleServiceContainer( _container );
        }

        public void AddExplicitRegisteredClass( Type type )
        {
            if( type == null ) throw new ArgumentNullException();
            if( _explicitRegisteredClasses == null ) _explicitRegisteredClasses = new List<Type>();
            _explicitRegisteredClasses.Add( type );
        }

        internal IReadOnlyList<Type> ExplicitRegisteredClasses =>_explicitRegisteredClasses;

        public IReadOnlyList<IStObjEngineAspect> Aspects => _aspects;

        public ISimpleServiceContainer ServiceContainer => _container;

        public void AddConfigureOnlyService<T>( ConfigureOnly<T> service ) => _configureOnlycontainer.Add( service );

        public StObjEngineConfigurator Configurator => _configurator;

        public void PushPostConfigureAction( Func<IActivityMonitor,IStObjEngineConfigurationContext,bool> postAction )
        {
            if( postAction == null ) throw new ArgumentNullException( nameof( postAction ) );
            _postActions.Add( postAction );
        }

        internal bool Initialize( IReadOnlyList<IStObjEngineAspectConfiguration> configs )
        {
            bool success = true;
            using( _monitor.OpenTrace().Send( $"Creating and configuring {configs.Count} aspect(s)." ) )
            {
                var aspectsType = new HashSet<Type>();
                foreach( var c in configs )
                {
                    if( c == null ) continue;
                    string aspectTypeName = null;
                    aspectTypeName = c.AspectType;
                    if( String.IsNullOrWhiteSpace( aspectTypeName ) )
                    {
                        success = false;
                        _monitor.Error( $"Null or empty {c.GetType().FullName}.AspectType string." );
                    }
                    else
                    {
                        // Registers the configuration instance itself.
                        _container.Add( c.GetType(), c, null );
                        Type t = SimpleTypeFinder.WeakResolver( aspectTypeName, true );
                        if( !aspectsType.Add( t ) )
                        {
                            success = false;
                            _monitor.Error().Send( $"Aspect '{t.FullName}' occurs more than once in configuration." );
                        }
                        else
                        {
                            IStObjEngineAspect a = CreateAspect( t );
                            if( a == null ) success = false;
                            else
                            {
                                using( _monitor.OpenTrace().Send( $"Configuring aspect '{t.FullName}'." ) )
                                {
                                    try
                                    {
                                        success |= a.Configure( _monitor, this );
                                    }
                                    catch( Exception ex )
                                    {
                                        success = false;
                                        _monitor.Error().Send( ex );
                                    }
                                }
                                if( success )
                                {
                                    // Adds the aspect itself to the container.
                                    _container.Add( t, a, null );
                                }
                            }
                        }
                    }
                }
                if( success ) success = ExecutePostActions();
            }
            return success;
        }

        IStObjEngineAspect CreateAspect( Type t )
        {
            {
                try
                {
                    var longestCtor = t.GetTypeInfo().GetConstructors()
                                        .Select( x => new
                                        {
                                            Ctor = x,
                                            P = x.GetParameters()
                                        } )
                                        // Ignores the default constructor.
                                        .Where( x => x.P.Length > 0 )
                                        .OrderByDescending( x => x.P.Length )
                                        .FirstOrDefault();
                    if( longestCtor == null )
                    {
                        _monitor.Error( $"Unable to find a non default constructor for Aspect '{t.FullName}'." );
                        return null;
                    }
                    var mapped = longestCtor.P.Select( param => new
                    {
                        Param = param,
                        Result = _configureOnlycontainer.GetService( param.ParameterType )
                    } );
                    if( mapped.Any( p => p.Result == null && !p.Param.HasDefaultValue ) )
                    {
                        using( _monitor.OpenError( $"Considering longest constructor: {longestCtor.ToString()}." ) )
                        {
                            foreach( var failed in mapped.Where( p => p.Result == null && !p.Param.HasDefaultValue ) )
                            {
                                _monitor.Trace( $"Resolution failed for parameter '{failed.Param.Name}', type: '{failed.Param.ParameterType.Name}'." );
                            }
                        }
                    }
                    var a = (IStObjEngineAspect)longestCtor.Ctor.Invoke( mapped.Select( p => p.Result ).ToArray() );
                    _aspects.Add( a );
                    return a;
                }
                catch( Exception ex )
                {
                    _monitor.Error().Send( ex, $"While creating aspect instance of '{t.FullName}'." );
                    return null;
                }
            }
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
                    _monitor.Error().Send( "A defered configuration action failed." );
                    return false;
                }
            }
            _postActions.Clear();
            return true;
        }

        public Action<IEnumerable<IDependentItem>> StObjDependencySorterHookInput
        {
            get { return _stObjDependencySorterHookInput; }
            set { _stObjDependencySorterHookInput = value; }
        }

        public Action<IEnumerable<ISortedItem>> StObjDependencySorterHookOutput
        {
            get { return _stObjDependencySorterHookOutput; }
            set { _stObjDependencySorterHookOutput = value; }
        }

    }
}
