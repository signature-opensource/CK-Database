using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CK.Setup
{
    /// <summary>
    /// Offers properties that may be set before the call to <see cref="SetupEngine.Run"/> or <see cref="SetupEngine.RunManual"/>.
    /// <see cref="VersionRepository"/> and <see cref="SetupSessionMemoryProvider"/> must be set, otherwise an <see cref="InvalidOperationException"/>
    /// will be thrown when running the engine.
    /// </summary>
    public sealed class SetupEngineStartConfiguration
    {
        readonly ScriptTypeManager _scriptTypeManager;
        readonly List<ISetupEngineAspect> _aspects;
        List<Type> _explicitRegisteredClasses;
        IVersionedItemRepository _versionRepository;
        ISetupSessionMemoryProvider _memory;
        Action<IEnumerable<IDependentItem>> _stObjDependencySorterHookInput;
        Action<IEnumerable<ISortedItem>> _stObjDependencySorterHookOutput;
        Action<IEnumerable<IDependentItem>> _dependencySorterHookInput;
        Action<IEnumerable<ISortedItem>> _dependencySorterHookOutput;

        internal SetupEngineStartConfiguration( SetupEngine e )
        {
            _scriptTypeManager = new ScriptTypeManager();
            _aspects = new List<ISetupEngineAspect>();
        }

        void CheckNotRunning( [CallerMemberName]string name = null )
        {
            if( _memory != null && _memory.IsStarted ) throw new InvalidOperationException( String.Format( "StartConfiguration.{0} must not be called while the engine is running.", name ) );
        }

        /// <summary>
        /// Gets the <see cref="ScriptTypeManager"/> into which <see cref="IScriptTypeHandler"/> must be registered
        /// before <see cref="SetupEngine.Run"/> in order for <see cref="ISetupScript"/> added to <see cref="SetupEngine.Scripts"/> to be executed.
        /// </summary>
        public ScriptTypeManager ScriptTypeManager
        {
            get { return _scriptTypeManager; }
        }

        /// <summary>
        /// Gets or sets a <see cref="IVersionedItemRepository">repository for version information</see>.
        /// It must be not null otherwise <see cref="SetupEngine.Run"/> or <see cref="SetupEngine.ManualRun"/> will raise an exception.
        /// </summary>
        public IVersionedItemRepository VersionRepository
        {
            get { return _versionRepository; }
            set { CheckNotRunning(); _versionRepository = value; }
        }

        /// <summary>
        /// Gets or sets a <see cref="ISetupSessionMemoryProvider">memory provider</see>.
        /// It must be not null otherwise <see cref="SetupEngine.Run"/> or <see cref="SetupEngine.ManualRun"/> will raise an exception.
        /// </summary>
        public ISetupSessionMemoryProvider SetupSessionMemoryProvider
        {
            get { return _memory; }
            set { CheckNotRunning(); _memory = value; }
        }

        /// <summary>
        /// Registers a type for registration.
        /// Aspects can use this instead of adding the assembly qualified name of the type into <see cref="BuildAndRegisterConfiguration.ExplicitClasses"/>.
        /// </summary>
        /// <param name="type">Type to register.</param>
        public void AddExplicitRegisteredClass( Type type )
        {
            
            if( type == null ) throw new ArgumentNullException();
            CheckNotRunning();
            if( _explicitRegisteredClasses == null ) _explicitRegisteredClasses = new List<Type>();
            _explicitRegisteredClasses.Add( type );
        }

        internal IReadOnlyList<Type> ExplicitRegisteredClasses
        {
            get { return _explicitRegisteredClasses; }
        }

        /// <summary>
        /// Gets the list of all registered aspects.
        /// Aspects are available in this list as soon as they are created (recall that the order 
        /// of the configurations in <see cref="SetupEngineConfiguration.Aspects"/> drives the order of Aspects creation).
        /// When <see cref="ISetupEngineAspect.Configure"/> is called, all available aspects are registered.
        /// </summary>
        /// <param name="type">Type to register.</param>
        public IReadOnlyList<ISetupEngineAspect> Aspects 
        {
            get { return _aspects; }
        }

        internal void AddAspect( ISetupEngineAspect a )
        {
            _aspects.Add( a );
        }

        /// <summary>
        /// Gets or sets a function that will be called with the list of StObjs once all of them are 
        /// registered in the <see cref="DependencySorter"/> used by the <see cref="StObjCollector"/>.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> StObjDependencySorterHookInput
        {
            get { return _stObjDependencySorterHookInput; }
            set { CheckNotRunning(); _stObjDependencySorterHookInput = value; }
        }

        /// <summary>
        /// Gets or sets a function that will be called when StObjs have been successfuly sorted by 
        /// the <see cref="DependencySorter"/> used by the <see cref="StObjCollector"/>.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> StObjDependencySorterHookOutput
        {
            get { return _stObjDependencySorterHookOutput; }
            set { CheckNotRunning(); _stObjDependencySorterHookOutput = value; }
        }

        /// <summary>
        /// Gets or sets a function that will be called with the list of items once all of them are registered.
        /// This can be used to dump detailed information about items registration and ordering.
        /// </summary>
        public Action<IEnumerable<IDependentItem>> DependencySorterHookInput
        {
            get { return _dependencySorterHookInput; }
            set { CheckNotRunning(); _dependencySorterHookInput = value; }
        }

        /// <summary>
        /// Gets or sets a function that will be called when items have been sorted.
        /// The final <see cref="DependencySorterResult"/> may not be successful (ie. <see cref="DependencySorterResult.HasStructureError"/> may be true),
        /// but if a cycle has been detected, this hook is not called.
        /// This can be used to dump detailed information about items registration and ordering.
        /// </summary>
        public Action<IEnumerable<ISortedItem>> DependencySorterHookOutput
        {
            get { return _dependencySorterHookOutput; }
            set { CheckNotRunning(); _dependencySorterHookOutput = value; }
        }

    }
}
