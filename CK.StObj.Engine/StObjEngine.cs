using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using System.IO;
using CK.Text;

namespace CK.Setup
{
    /// <summary>
    /// Generic engine that runs a <see cref="StObjEngineConfiguration"/>.
    /// </summary>
    public class StObjEngine
    {
        readonly IActivityMonitor _monitor;
        readonly StObjEngineConfiguration _config;
        readonly XElement _ckSetupConfig;
        readonly IStObjRuntimeBuilder _runtimeBuilder;
        Status _status;
        StObjEngineConfigureContext _startContext;

        class Status : IStObjEngineStatus, IDisposable
        {
            readonly IActivityMonitor _m;
            readonly ActivityMonitorPathCatcher _pathCatcher;
            public bool Success;

            public Status( IActivityMonitor m )
            {
                _m = m;
                _pathCatcher = new ActivityMonitorPathCatcher() { IsLocked = true };
                _m.Output.RegisterClient( _pathCatcher );
                Success = true;
            }

            bool IStObjEngineStatus.Success => Success;

            public IReadOnlyList<ActivityMonitorPathCatcher.PathElement> DynamicPath => _pathCatcher.DynamicPath;

            public IReadOnlyList<ActivityMonitorPathCatcher.PathElement> LastErrorPath => _pathCatcher.LastErrorPath;

            public IReadOnlyList<ActivityMonitorPathCatcher.PathElement> LastWarnOrErrorPath => _pathCatcher.LastErrorPath;

            public void Dispose()
            {
                _pathCatcher.IsLocked = false;
                _m.Output.UnregisterClient( _pathCatcher );
            }
        }

        /// <summary>
        /// Initializes a new <see cref="StObjEngine"/>.
        /// </summary>
        /// <param name="monitor">Logger that must be used.</param>
        /// <param name="config">Configuration that describes the key aspects of the build.</param>
        /// <param name="runtimeBuilder">The object in charge of actual objects instanciation. When null, <see cref="StObjContextRoot.DefaultStObjRuntimeBuilder"/> is used.</param>
        public StObjEngine( IActivityMonitor monitor, StObjEngineConfiguration config, IStObjRuntimeBuilder runtimeBuilder = null )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( config == null ) throw new ArgumentNullException( nameof( config ) );
            _monitor = monitor;
            _config = config;
            _runtimeBuilder = runtimeBuilder ?? StObjContextRoot.DefaultStObjRuntimeBuilder;
        }

        /// <summary>
        /// Initializes a new <see cref="StObjEngine"/> from a xml element (see <see cref="StObjEngineConfiguration(XElement)"/>).
        /// </summary>
        /// <param name="monitor">Logger that must be used.</param>
        /// <param name="config">Configuration that describes the key aspects of the build.</param>
        public StObjEngine( IActivityMonitor monitor, XElement config )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( config == null ) throw new ArgumentNullException( nameof( config ) );
            _monitor = monitor;
            _runtimeBuilder = StObjContextRoot.DefaultStObjRuntimeBuilder;
            _config = new StObjEngineConfiguration( config );
            _ckSetupConfig = config.Element( "CKSetup" );
        }

        /// <summary>
        /// Gets whether this engine is running or has <see cref="Run"/> (it can run only once).
        /// </summary>
        public bool Started => _startContext != null;

        class BinPathComparer : IEqualityComparer<BinPath>
        {
            public static BinPathComparer Default = new BinPathComparer();

            public bool Equals( BinPath x, BinPath y )
            {
                return x.Types.SetEquals( y.Types )
                        && x.Assemblies.SetEquals( y.Assemblies )
                        && x.ExcludedTypes.SetEquals( y.ExcludedTypes )
                        && x.ExternalScopedTypes.SetEquals( y.ExternalScopedTypes )
                        && x.ExternalSingletonTypes.SetEquals( y.ExternalSingletonTypes );
            }

            public int GetHashCode( BinPath b ) => b.ExcludedTypes.Count
                                                    + b.ExternalScopedTypes.Count * 37
                                                    + b.Types.Count * 59
                                                    + b.ExternalSingletonTypes.Count * 83
                                                    + b.Assemblies.Count * 117;
        }

        /// <summary>
        /// Runs the setup.
        /// </summary>
        /// <returns>True on success, false if an error occurred.</returns>
        public bool Run()
        {
            if( _startContext != null ) throw new InvalidOperationException( "Run can be called only once." );
            if( !RootBinPathsAndOutputPaths() ) return false;
            if( _ckSetupConfig != null && !ApplyCKSetupConfiguration() ) return false;

            var rootBinPath = new BinPath();
            rootBinPath.Path = AppContext.BaseDirectory;
            rootBinPath.Assemblies.AddRange( _config.BinPaths.SelectMany( b => b.Assemblies ) );
            rootBinPath.Types.AddRange( _config.BinPaths.SelectMany( b => b.Types ) );
            rootBinPath.ExcludedTypes.AddRange( _config.GlobalExcludedTypes );
            foreach( var f in _config.BinPaths ) f.ExcludedTypes.AddRange( rootBinPath.ExcludedTypes );
            // Unifies External lifetime definition: choose Scope as soon as one BinPath want Scope.
            // Unifies also all the Singletons but remove any Scoped from them... This is not perfect
            // but should bo the kob in practice.
            rootBinPath.ExternalScopedTypes.AddRange( _config.BinPaths.SelectMany( b => b.ExternalScopedTypes ) );
            rootBinPath.ExternalSingletonTypes.AddRange( _config.BinPaths.SelectMany( b => b.ExternalSingletonTypes ).Except( rootBinPath.ExternalScopedTypes ) );
            rootBinPath.GenerateSourceFiles = false;
            rootBinPath.SkipCompilation = true;

            // Groups similar configurations to optimize runs.
            var groups = _config.BinPaths.Append( rootBinPath ).GroupBy( Util.FuncIdentity, BinPathComparer.Default ).ToList();
            var rootGroup = groups.Single( g => g.Contains( rootBinPath ) );

            _status = new Status( _monitor );
            _startContext = new StObjEngineConfigureContext( _monitor, _config, _status );
            try
            {
                _startContext.CreateAndConfigureAspects( _config.Aspects, () => _status.Success = false );
                if( _status.Success )
                {
                    StObjCollectorResult firstRun = SafeBuildStObj( rootBinPath, null );
                    if( firstRun == null ) return _status.Success = false;

                    var runCtx = new StObjEngineRunContext( _monitor, _startContext, firstRun.OrderedStObjs, firstRun.Features );
                    runCtx.RunAspects( () => _status.Success = false );

                    if( _status.Success )
                    {
                        string dllName = _config.GeneratedAssemblyName;
                        if( !dllName.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) ) dllName += ".dll";

                        using( _monitor.OpenInfo( "Generating AppContext assembly (first run)." ) )
                        {
                            _status.Success = CodeGenerationForPaths( rootGroup, firstRun, dllName );
                        }
                        if( _status.Success )
                        {
                            foreach( var g in groups.Where( g => g != rootGroup ) )
                            {
                                if( !SecondaryCodeGeneration( firstRun, dllName, g ) )
                                {
                                    _status.Success = false;
                                    break;
                                }
                            }
                        }
                    }
                    if( !_status.Success )
                    {
                        var errorPath = _status.LastErrorPath;
                        if( errorPath == null || errorPath.Count == 0 )
                        {
                            _monitor.Fatal( "Success status is false but no error has been logged." );
                        }
                    }
                    var termCtx = new StObjEngineTerminateContext( _monitor, runCtx );
                    termCtx.TerminateAspects( () => _status.Success = false );
                }
                return _status.Success;
            }
            finally
            {
                DisposeDisposableAspects();
                _status.Dispose();
            }
        }

        private bool RootBinPathsAndOutputPaths()
        {
            if( _ckSetupConfig != null )
            {
                if( _config.BasePath.IsEmptyPath )
                {
                    _config.BasePath = (string)_ckSetupConfig.Element( StObjEngineConfiguration.XmlNames.BasePath );
                    if( _config.BasePath.IsEmptyPath ) _monitor.Trace( "No BasePath defined: using CKSetup BasePath." );
                }
            }
            if( _config.BasePath.IsEmptyPath )
            {
                _config.BasePath = Environment.CurrentDirectory;
                _monitor.Info( $"No BasePath. Using current directory '{_config.BasePath}'." );
            }
            foreach( var b in _config.BinPaths )
            {
                if( b == null )
                {
                    _monitor.Error( "Null BinPath found." );
                    return false;
                }
                if( !b.Path.IsRooted ) b.Path = _config.BasePath.Combine( b.Path );
                b.Path = b.Path.ResolveDots();

                if( b.OutputPath.IsEmptyPath ) b.OutputPath = b.Path;
                else if( !b.OutputPath.IsRooted ) b.OutputPath = _config.BasePath.Combine( b.Path );
                b.OutputPath = b.OutputPath.ResolveDots();
            }
            return true;
        }

        bool ApplyCKSetupConfiguration()
        {
            using( _monitor.OpenInfo( "Applying CKSetup configuration." ) )
            {
                var binPaths = _ckSetupConfig.Element( StObjEngineConfiguration.XmlNames.BinPaths );
                if( binPaths == null )
                {
                    _monitor.Error( "Missing CKSetup/BinPaths element." );
                    return false;
                }
                var matched = _config.BinPaths.Select( b => (M: b, P: new NormalizedPath()) ).ToList();
                int initialCount = matched.Count;
                foreach( var ckSetupBinPath in binPaths.Elements( StObjEngineConfiguration.XmlNames.BinPath ) )
                {
                    NormalizedPath directory = (string)ckSetupBinPath.Attribute( StObjEngineConfiguration.XmlNames.BinPath );
                    var assemblies = ckSetupBinPath.Elements()
                                        .Where( e => e.Name == "Model" || e.Name == "ModelDependent" )
                                        .Select( e => e.Value );

                    var match = matched.Where( b => b.M.Path.StartsWith( directory, false ) ).ToArray();
                    if( match.Length > 1 )
                    {
                        _monitor.Error( $"Ambiguous match for CKSetup BinPath '{directory}': {match.Select( b => b.M.Path.Path ).Concatenate()} " );
                        return false;
                    }
                    if( match.Length == 1 )
                    {
                        if( match[0].P.IsEmptyPath )
                        {
                            match[0].P = directory;
                            var a = match[0].M.Assemblies;
                            int aCount = a.Count;
                            a.AddRange( assemblies );
                            _monitor.Info( $"CKStup BinPath '{directory}' matched. Added {a.Count - aCount} assemblies." );
                        }
                        else
                        {
                            _monitor.Error( $"Ambiguous BinPath match between '{directory}' and '{match[0].P}'." );
                            return false;
                        }
                    }
                    else
                    {
                        _monitor.Info( $"CKStup BinPath '{directory}' not matched. Creating new default BinPath." );
                        var b = new BinPath();
                        b.Assemblies.AddRange( assemblies );
                        _config.BinPaths.Add( b );
                        matched.Add( (b, directory) );
                    }
                }
                var noMatch = matched.Where( b => b.P.IsEmptyPath );
                if( noMatch.Any() )
                {
                    if( initialCount == matched.Count )
                    {
                        _monitor.Warn( $"Missing match for BinPath: {noMatch.Select( b => b.P.Path ).Concatenate()}." );
                    }
                    else
                    {
                        _monitor.Error( $"BinPath '{noMatch.Select( b => b.P.Path ).Concatenate( "', '" )}' not matched and at the same time CKSetup BinPath '{matched.Skip( initialCount ).Select( b => b.P.Path ).Concatenate( "', '" )}' have been created. This must be corrected." );
                        return false;
                    }
                }
            }
            return true;
        }


        bool SecondaryCodeGeneration( StObjCollectorResult firstRunResult, string dllName, IGrouping<BinPath,BinPath> binPaths )
        {
            using( _monitor.OpenInfo( $"Generating assembly for BinPaths '{binPaths.Select( b => b.Path.Path ).Concatenate("', '")}'." ) )
            {
                var head = binPaths.Key;
                StObjCollectorResult rFolder = SafeBuildStObj( head, firstRunResult.SecondaryRunAccessor );
                if( rFolder == null ) return false;
                return CodeGenerationForPaths( binPaths, rFolder, dllName );
            }
        }

        bool CodeGenerationForPaths( IGrouping<BinPath, BinPath> binPaths, StObjCollectorResult r, string dllName )
        {
            var head = binPaths.Key;
            var g = r.GenerateFinalAssembly( _monitor, head.OutputPath.AppendPart( dllName ), binPaths.Any( f => f.GenerateSourceFiles ), _config.InformationalVersion, binPaths.All( f => f.SkipCompilation ) );
            if( g.GeneratedFileNames.Count > 0 )
            {
                foreach( var f in binPaths )
                {
                    if( !f.GenerateSourceFiles && f.SkipCompilation ) continue;
                    var dir = f.OutputPath;
                    if( dir == head.OutputPath ) continue;
                    using( _monitor.OpenInfo( $"Copying generated files to folder: '{dir}'." ) )
                    {
                        foreach( var file in g.GeneratedFileNames )
                        {
                            if( file == dllName )
                            {
                                if( f.SkipCompilation ) continue;
                            }
                            else
                            {
                                if( !f.GenerateSourceFiles ) continue;
                            }
                            try
                            {
                                _monitor.Info( file );
                                File.Copy( head.OutputPath.Combine( file ), dir.Combine( file ), true );
                            }
                            catch( Exception ex )
                            {
                                _monitor.Error( ex );
                                return false;
                            }
                        }
                    }
                }
            }
            return g.Success;
        }

        class TypeFilterFromConfiguration : IStObjTypeFilter
        {
            readonly StObjConfigurationLayer _firstLayer;
            readonly HashSet<string> _excludedTypes;

            public TypeFilterFromConfiguration( BinPath f, StObjConfigurationLayer firstLayer )
            {
                _excludedTypes = f.ExcludedTypes;
                _firstLayer = firstLayer;
            }

            bool IStObjTypeFilter.TypeFilter( IActivityMonitor monitor, Type t )
            {
                if( _excludedTypes.Contains( t.Name ) )
                {
                    monitor.Info( $"Type {t.AssemblyQualifiedName} is filtered out by its Type Name." );
                    return false;
                }
                if( _excludedTypes.Contains( t.FullName ) )
                {
                    monitor.Info( $"Type {t.AssemblyQualifiedName} is filtered out by its Type FullName." );
                    return false;
                }
                if( _excludedTypes.Contains( t.AssemblyQualifiedName ) )
                {
                    monitor.Info( $"Type {t.AssemblyQualifiedName} is filtered out by its Type AssemblyQualifiedName." );
                    return false;
                }
                if( SimpleTypeFinder.WeakenAssemblyQualifiedName( t.AssemblyQualifiedName, out var weaken )
                    && _excludedTypes.Contains( weaken ) )
                {
                    monitor.Info( $"Type {t.AssemblyQualifiedName} is filtered out by its weak type name ({weaken})." );
                    return false;
                }
                return _firstLayer.TypeFilter( monitor, t );
            }
        }

        StObjCollectorResult SafeBuildStObj( BinPath f, Func<string,object> secondaryRunAccessor )
        {
            bool hasError = false;
            using( _monitor.OnError( () => hasError = true ) )
            using( _monitor.OpenInfo( "Building StObj objects." ) )
            {
                StObjCollectorResult result;
                var configurator = _startContext.Configurator.FirstLayer;
                var typeFilter = new TypeFilterFromConfiguration( f, configurator );
                StObjCollector stObjC = new StObjCollector(
                    _monitor,
                    _startContext.ServiceContainer,
                    _config.TraceDependencySorterInput,
                    _config.TraceDependencySorterOutput,
                    _runtimeBuilder,
                    typeFilter, configurator, configurator,
                    secondaryRunAccessor );
                stObjC.RevertOrderingNames = _config.RevertOrderingNames;
                using( _monitor.OpenInfo( "Registering types." ) )
                {
                    if( f.ExternalSingletonTypes.Count != 0 )
                    {
                        stObjC.DefineAsExternalSingletons( f.ExternalSingletonTypes );
                    }
                    if( f.ExternalScopedTypes.Count != 0 )
                    {
                        stObjC.DefineAsExternalScoped( f.ExternalScopedTypes );
                    }
                    stObjC.RegisterAssemblyTypes( f.Assemblies );
                    stObjC.RegisterTypes( f.Types );
                    foreach( var t in _startContext.ExplicitRegisteredTypes ) stObjC.RegisterType( t );
                    Debug.Assert( stObjC.RegisteringFatalOrErrorCount == 0 || hasError, "stObjC.RegisteringFatalOrErrorCount > 0 ==> An error has been logged." );
                }
                if( stObjC.RegisteringFatalOrErrorCount == 0 )
                {
                    using( _monitor.OpenInfo( "Resolving StObj dependency graph." ) )
                    {
                        result = stObjC.GetResult();
                        Debug.Assert( !result.HasFatalError || hasError, "result.HasFatalError ==> An error has been logged." );
                    }
                    if( !result.HasFatalError ) return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Disposes all disposable aspects.
        /// </summary>
        void DisposeDisposableAspects()
        {
            foreach( var aspect in _startContext.Aspects.OfType<IDisposable>() )
            {
                try
                {
                    aspect.Dispose();
                }
                catch( Exception ex )
                {
                    _monitor.Error( $"While disposing Aspect '{aspect.GetType().AssemblyQualifiedName}'.", ex );
                }
            }
        }

    }
}
