using CK.Core;
using CK.Text;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    /// <summary>
    /// Immutable collection of <see cref="Component"/>.
    /// </summary>
    public class ComponentDB
    {
        readonly IComponentDBEventSink _sink;

        /// <summary>
        /// Initializes a new mpty <see cref="ComponentDB"/>.
        /// </summary>
        /// <param name="sink">The sink for the events. Can be null if changes don't need to be tracked.</param>
        public ComponentDB( IComponentDBEventSink c )
        {
            _sink = c;
            Components = Array.Empty<Component>();
        }

        /// <summary>
        /// Initializes a new <see cref="ComponentDB"/> from its <see cref="XElement"/> representation.
        /// </summary>
        /// <param name="sink">The sink for the events. Can be null if changes don't need to be tracked.</param>
        /// <param name="e">The xml element.</param>
        public ComponentDB( IComponentDBEventSink sink, XElement e )
        {
            _sink = sink;
            var comps = new List<Component>();
            Components = comps;
            foreach( var c in e.Elements( DBXmlNames.Component ) )
            {
                comps.Add( new Component( c ) );
            }
        }

        ComponentDB( IComponentDBEventSink sink, IEnumerable<Component> components )
        {
            _sink = sink;
            Components = components.ToArray();
        }

        /// <summary>
        /// Creates a <see cref="XElement"/> representation of this database.
        /// </summary>
        /// <returns>The Xml element.</returns>
        public XElement ToXml()
        {
            return new XElement( DBXmlNames.DB, Components.Select( c => c.ToXml() ) );
        }

        /// <summary>
        /// Gets the list of registered components.
        /// </summary>
        public IReadOnlyList<Component> Components { get; }

        /// <summary>
        /// Gets a set of <see cref="ComponentRef"/> that should be added since they are
        /// currently discovered as embedded inside other ones.
        /// </summary>
        public IEnumerable<ComponentRef> EmbeddedComponents => Components.SelectMany( c => c.Embedded ).Distinct();


        /// <summary>
        /// Tries to finds the best available component for the given runtime, and a 
        /// minimal version.
        /// </summary>
        /// <param name="runtime">The runtime to consider.</param>
        /// <param name="name">The component name.</param>
        /// <param name="minVersion">Optional minimal version to satisfy.</param>
        /// <returns>The Component or null if not found.</returns>
        public Component FindBest( TargetRuntime runtime, string name, SVersion minVersion )
        {
            return Components
                    .Where( c => c.TargetFramework.CanWorkOn( runtime )
                                    && c.Name == name
                                    && c.Version >= minVersion )
                    .OrderBy( c => c.Version )
                    .ThenBy( c => c.TargetFramework )
                    .FirstOrDefault();
        }

        /// <summary>
        /// Finds a <see cref="ComponentRef"/>, returning null if not found.
        /// </summary>
        /// <param name="r">The component reference to find.</param>
        /// <returns>The registered component or null.</returns>
        public Component Find( ComponentRef r ) => Components.FirstOrDefault( c => c.Is( r ) );

        /// <summary>
        /// Finds a <see cref="ComponentRef"/> or throws a <see cref="InvalidOperationException"/>
        /// stating that the component is not registered.
        /// </summary>
        /// <param name="r">The component reference to find.</param>
        /// <returns>The registered component.</returns>
        public Component FindRequired( ComponentRef r )
        {
            Component c = Find( r );
            if( c == null ) throw new InvalidOperationException( $"Component '{r}' is not registered." );
            return c;
        }

        /// <summary>
        /// Registers a bin folder. 
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="folder">The folder to register.</param>
        /// <returns>Null on error, this <see cref="ComponentDB"/> if no changes occured or a new database.</returns>
        public ComponentDB Add( IActivityMonitor m, BinFolder folder )
        {
            if( !folder.Heads.Any() )
            {
                m.Error().Send( "No components found." );
                return null;
            }
            var freeHeads = folder.Heads.Where( h => Find( h.ComponentRef ) == null );
            int freeHeadsCount = freeHeads.Count();
            if( freeHeadsCount > 1 )
            {
                m.Error().Send( $"Cannot register '{freeHeads.Select( h => h.Name.Name ).Concatenate( "', '" )}' at the same time. They must be registered individually." );
                return null;
            }
            if( freeHeadsCount == 0 )
            {
                m.Warn().Send( $"No component added (found already registered Components: '{folder.Heads.Select( h => h.Name.Name ).Concatenate( "', '" )}')" );
                return this;
            }
            BinFileInfo toAdd = freeHeads.Single();
            using( m.OpenInfo().Send( $"Found '{toAdd.ComponentRef.EntryPathPrefix}' to register." ) )
            {
                List<ComponentDependency> dependencies = CollectSetupDependencies( m, toAdd.SetupDependencies );

                var embeddedComponents = new List<ComponentRef>();
                var files = folder.Files.Select( f => f.LocalFileName );
                foreach( var sub in folder.Components )
                {
                    if( sub == toAdd ) continue;
                    var cSub = Find( sub.ComponentRef );
                    if( cSub != null )
                    {
                        if( dependencies.Any( d => d.UseName == cSub.Name ) )
                        {
                            m.Error().Send( $"{cSub.Name} is declared as a Setup dependency but exists as an embedded component." );
                            return null;
                        }
                        if( toAdd.ComponentKind != ComponentKind.Model && cSub.ComponentKind != ComponentKind.Model )
                        {
                            dependencies.Add( new ComponentDependency( cSub.Name, cSub.Version ) );
                        }
                        m.Info().Send( $"Removing {cSub.Files.Count} files thanks to already registered '{cSub.GetRef()}'." );
                        files = files.Where( f => !cSub.Files.Contains( f ) );
                    }
                    else
                    {
                        m.Warn().Send( $"Embedded component '{sub.ComponentRef}' will be included. It should be registered individually." );
                        embeddedComponents.Add( sub.ComponentRef );
                    }
                }
                var newC = new Component( toAdd.ComponentKind, toAdd.ComponentRef, dependencies, embeddedComponents, files );
                _sink?.ComponentLocallyAdded( newC, folder );
                return DoAdd( m, newC );
            }
        }

        ComponentDB DoAdd( IActivityMonitor m, Component newC ) => new ComponentDB( _sink, Components.Select( c => c.WithNewComponent( _sink, m, newC ) ).Append( newC ) );

        /// <summary>
        /// Exports a filtered set of components to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="filter">Filter for components to export.</param>
        /// <param name="fileWriter">Async file writer function.</param>
        /// <param name="output">Output stream.</param>
        /// <param name="cancellation">Optional cancellation token.</param>
        public async Task Export(
            Func<Component, ComponentExportType> filter,
            Func<ComponentRef, string, Stream, CancellationToken, Task> fileWriter,
            Stream output,
            CancellationToken cancellation = default( CancellationToken ) )
        {
            CKBinaryWriter writer = new CKBinaryWriter( output );
            writer.WriteNonNegativeSmallInt32( 0 );
            foreach( var c in Components )
            {
                var type = filter( c );
                if( type == ComponentExportType.None ) continue;
                writer.Write( (byte)type );
                writer.Write( c.ToXml().ToString( SaveOptions.DisableFormatting ) );
                if( type == ComponentExportType.DescriptionAndFiles )
                {
                    foreach( var f in c.Files )
                    {
                        await fileWriter( c.GetRef(), f, output, cancellation );
                        cancellation.ThrowIfCancellationRequested();
                    }
                }
            }
            writer.Write( (byte)ComponentExportType.None );
        }

        /// <summary>
        /// Imports a set of components from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="fileReader">Async file reader function.</param>
        /// <param name="input">Output stream.</param>
        /// <param name="cancellation">Optional cancellation token.</param>
        /// <returns>The new ComponentDB with imported components.</returns>
        public async Task<ComponentDB> Import(
            IActivityMonitor monitor,
            Func<ComponentRef, string, bool, Stream, CancellationToken, Task> fileReader,
            Stream input,
            CancellationToken cancellation = default( CancellationToken ) )
        {
            using( monitor.OpenInfo().Send( "Starting components import." ) )
            {
                ComponentDB currentDb = this;
                try
                {
                    CKBinaryReader reader = new CKBinaryReader( input );
                    var v = reader.ReadNonNegativeSmallInt32();
                    monitor.Debug().Send( $"Stream version: {v}" );
                    ComponentExportType type;
                    while( (type = (ComponentExportType)reader.ReadByte()) != ComponentExportType.None )
                    {
                        var newC = new Component( XElement.Parse( reader.ReadString() ) );
                        if( type == ComponentExportType.DescriptionAndFiles )
                        {
                            bool skip = Find( newC.GetRef() ) != null;
                            if( skip )
                            {
                                monitor.Trace().Send( $"Skipping '{newC}' since it already exists." );
                            }
                            else
                            {
                                monitor.Trace().Send( $"Importing Component '{newC}' ({newC.Files.Count} files)." );
                            }
                            foreach( var f in newC.Files )
                            {
                                await fileReader( newC.GetRef(), f, skip, input, cancellation );
                                cancellation.ThrowIfCancellationRequested();
                            }
                        }
                        currentDb = DoAdd( monitor, newC );
                    }
                }
                catch( OperationCanceledException )
                {
                    throw;
                }
                catch( Exception ex )
                {
                    monitor.Error().Send( ex );
                    return null;
                }
                return currentDb;
            }
        }

        /// <summary>
        /// Returns a new <see cref="DependencyResolver"/> for a target <see cref="BinFolder"/>.
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="target">The targets for which required components must be found.</param>
        /// <returns>Null on error, otherwise the DependencyResolver (may be empty).</returns>
        public DependencyResolver GetRuntimeDependenciesResolver( IActivityMonitor m, IEnumerable<BinFolder> targets )
        {
            using( m.OpenInfo().Send( $"Creating runtime dependencies resolver for {targets.Select( t => t.BinPath ).Concatenate()}." ) )
            {
                var models = targets.SelectMany( t => t.Components ).Where( c => c.ComponentKind == ComponentKind.Model );
                if( !models.Any() )
                {
                    m.Warn().Send( "No Model component found." );
                    return new DependencyResolver( this, TargetRuntime.None, Array.Empty<ComponentDependency>() );
                }
                foreach( var eOrR in targets.SelectMany( t => t.Components ).Where( c => c.ComponentKind != ComponentKind.Model ) )
                {
                    m.Warn().Send( $"{eOrR.ComponentKind} '{eOrR.ComponentRef}' found. It will be ignored." );
                }
                var targetRuntime = SelectTargetRuntime( m, models );
                if( targetRuntime == TargetRuntime.None ) return null;

                var rootDeps = CollectSetupDependencies( m, models.SelectMany( b => b.SetupDependencies ) );
                return new DependencyResolver( this, targetRuntime, rootDeps );
            }
        }

        ///// <summary>
        ///// Resolves required runtime components for a target <see cref="BinFolder"/>.
        ///// Models are skipped here.
        ///// </summary>
        ///// <param name="m">The monitor to use.</param>
        ///// <param name="target">The target for which required components must be found.</param>
        ///// <returns>Null on error, otherwise the components to install (may be empty).</returns>
        //public IReadOnlyList<Component> ResolveRuntimeDependencies( IActivityMonitor m, BinFolder target )
        //{
        //    using( m.OpenInfo().Send( $"Resolving runtime dependencies for {target.BinPath}." ) )
        //    {
        //        var models = target.Components.Where( c => c.ComponentKind == ComponentKind.Model );
        //        if( !models.Any() )
        //        {
        //            m.Warn().Send( "No Model component found." );
        //            return Array.Empty<Component>();
        //        }
        //        foreach( var eOrR in target.Components.Where( c => c.ComponentKind != ComponentKind.Model ) )
        //        {
        //            m.Warn().Send( $"{eOrR.ComponentKind} '{eOrR.ComponentRef}' found. It will be ignored." );
        //        }
        //        var targetRuntime = SelectTargetRuntime( m, models );
        //        if( targetRuntime == TargetRuntime.None ) return null;

        //        List<ComponentDependency> rootDeps = CollectSetupDependencies( m, models.SelectMany( b => b.SetupDependencies ) );

        //        var installableComponents = GetInstallableComponents( m, rootDeps, targetRuntime );
        //        if( installableComponents == null ) return null;

        //        var toInstall = installableComponents.ResolveMinimalVersions( m );
        //        if( toInstall == null ) return null;

        //        m.CloseGroup( toInstall.Count == 0 ? "No component found." : $"{toInstall.Count} components fond." );
        //        return toInstall;
        //    }
        //}

        //class InstallableComponents
        //{
        //    readonly ComponentDB _db;
        //    readonly TargetFramework _targetFramework;
        //    readonly List<Component> _components;
        //    readonly int _initialCount;

        //    public InstallableComponents( ComponentDB db, TargetFramework targetFramework, IEnumerable<Component> c )
        //    {
        //        _db = db;
        //        _targetFramework = targetFramework;
        //        _components = c.ToList();
        //        _initialCount = _components.Count;
        //    }

        //    public List<Component> ResolveMinimalVersions( IActivityMonitor m )
        //    {
        //        int idx = 0;
        //        do
        //        {
        //            foreach( var dep in _components[idx].Dependencies )
        //            {
        //                Component registered;
        //                Component exactRegistered = _db.Components.SingleOrDefault( c => c.Name == dep.UseName && c.TargetFramework == _targetFramework && c.Version == dep.UseMinVersion );
        //                if( exactRegistered != null ) registered = exactRegistered;
        //                else
        //                {
        //                    Component best = _db.Components.Where( c => c.Name == dep.UseName && _targetFramework.CanWorkWith( c.TargetFramework ) && c.Version >= dep.UseMinVersion )
        //                                        .OrderByDescending( c => c.Version )
        //                                        .FirstOrDefault();
        //                    if( best == null )
        //                    {
        //                        m.Error().Send( $"Component {dep.UseName} in version at least {dep.UseMinVersion} is required." );
        //                        return null;
        //                    }
        //                    registered = best;
        //                    m.Warn().Send( $"Component {dep.UseName}/{dep.UseMinVersion} is not registered, upgrading to {best.GetRef()}." );
        //                }
        //                Debug.Assert( registered.ComponentKind != ComponentKind.Model );

        //                int alreadyHereIdx = _components.IndexOf( c => c.Name == dep.UseName );
        //                if( alreadyHereIdx < 0 )
        //                {
        //                    // This is a new Component.
        //                    _components.Add( registered );
        //                }
        //                else 
        //                {
        //                    // If the required version is lower or equal to the already choosen component,
        //                    // we have nothing to do.
        //                    if( dep.UseMinVersion > _components[alreadyHereIdx].Version )
        //                    {
        //                        // We always update the list with the new (greater) version of the component.
        //                        _components[idx] = registered;
        //                        // Did we already processed the depependencies of this component?
        //                        if( alreadyHereIdx > idx )
        //                        {
        //                            // Yes: we have to restart the whole process.
        //                            // We keep at least our initial count or, if we made progress,
        //                            // the current successfuly processed set.
        //                            int succesfulIndex = Math.Max( idx+1, _initialCount );
        //                            var r = new InstallableComponents( _db, _targetFramework, _components.Take( succesfulIndex ) );
        //                            return r.ResolveMinimalVersions( m );
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        while( ++idx < _components.Count );
        //        return _components;
        //    }
        //}

        //private InstallableComponents GetInstallableComponents( IActivityMonitor m, IEnumerable<ComponentDependency> rootDeps, TargetFramework targetFramework )
        //{
        //    var resolved = rootDeps.Select( d => new RequiredDependency(
        //                        d,
        //                        Components
        //                            .Where( c => targetFramework.CanWorkWith( c.TargetFramework ) 
        //                                            && c.Name == d.UseName 
        //                                            && c.Version >= d.UseMinVersion )
        //                            .OrderByDescending( c => c.Version )
        //                            .ThenBy( c => c.TargetFramework )
        //                            .FirstOrDefault()
        //                        ) );
        //    var missing = resolved.Where( r => r.Component == null );
        //    if( missing.Any() )
        //    {
        //        foreach( var missed in missing )
        //        {
        //            var dep = missed.Dependency;
        //            if( dep.UseMinVersion != null )
        //                m.Error().Send( $"Unable to find component {dep.UseName} compatible with {targetFramework} with version at least {dep.UseMinVersion}." );
        //            else m.Error().Send( $"Unable to find component {dep.UseName} compatible with {targetFramework} (any version)." );
        //            return null;
        //        }
        //    }
        //    return new InstallableComponents( this, targetFramework, resolved.Select( r => r.Component ) );
        //}

        static TargetRuntime SelectTargetRuntime( IActivityMonitor m, IEnumerable<BinFileInfo> models )
        {
            using( m.OpenInfo().Send( $"Detecting runtimes for: ${ models.Select( x => x.Name.Name + '/' + x.ComponentRef.TargetFramework ).Concatenate() }" ) )
            {
                var runtimes = models.First().ComponentRef.TargetFramework.GetCommonRuntimes( models.Skip( 1 ).Select( x => x.ComponentRef.TargetFramework ) );
                if( !runtimes.Any() )
                {
                    m.Error().Send( $"Unable to determine at least one common allowed runtime." );
                    return TargetRuntime.None;
                }
                var theOnlyOne = runtimes.Count() == 1 ? runtimes.First() : TargetRuntime.None;
                if( theOnlyOne != TargetRuntime.None )
                {
                    m.CloseGroup( $"Single selected runtime: {theOnlyOne}." );
                    return theOnlyOne;
                }
                m.Info().Send( $"Multiple possible runtime: {runtimes.Select( r => r.ToString() ).Concatenate()}." );
                theOnlyOne = runtimes.Min();
                m.CloseGroup( $"Lowest selected runtime: {theOnlyOne}." );
                return theOnlyOne;
            }
        }

        static List<ComponentDependency> CollectSetupDependencies(IActivityMonitor m, IEnumerable<SetupDependency> deps)
        {
            var dependencies = new List<ComponentDependency>();
            foreach( var dep in deps.GroupBy( d => d.UseName ) )
            {
                string name = dep.Key;
                var versions = dep.Where( d => d.UseMinVersion != null ).Select( d => d.UseMinVersion ).Distinct().ToList();
                if( versions.Count == 0 )
                {
                    dependencies.Add( new ComponentDependency( name, null ) );
                }
                else if( versions.Count == 1 )
                {
                    dependencies.Add( new ComponentDependency( name, versions[0] ) );
                }
                else
                {
                    var max = versions.Max();
                    var culprits = dep.Where( d => d.UseName == name );
                    using( m.OpenWarn().Send( $"Version upgrade for '{name}'. Using: {max}." ) )
                    {
                        foreach( var c in culprits )
                        {
                            m.Warn().Send( $"'{c.Source.Name.Name}' declares to use the version {c.UseMinVersion}." );
                        }
                    }
                    dependencies.Add( new ComponentDependency( name, max ) );
                }
            }
            return dependencies;
        }
    }
}
