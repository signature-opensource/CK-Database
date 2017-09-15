using CK.Core;
using CK.Text;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        /// <summary>
        /// Initializes a new mpty <see cref="ComponentDB"/>.
        /// </summary>
        public ComponentDB()
        {
            Components = Array.Empty<Component>();
        }

        /// <summary>
        /// Initializes a new <see cref="ComponentDB"/> from its <see cref="XElement"/> representation.
        /// </summary>
        /// <param name="sink">The sink for the events. Can be null if changes don't need to be tracked.</param>
        /// <param name="e">The xml element.</param>
        public ComponentDB( XElement e )
        {
            var comps = new List<Component>();
            Components = comps;
            foreach( var c in e.Elements( DBXmlNames.Component ) )
            {
                comps.Add( new Component( c ) );
            }
            Version = (long)e.Attribute( DBXmlNames.Version );
        }

        ComponentDB( ComponentDB origin, IEnumerable<Component> components )
        {
            Components = components.ToArray();
            Version = origin.Version + 1;
        }

        /// <summary>
        /// Creates a <see cref="XElement"/> representation of this database.
        /// </summary>
        /// <returns>The Xml element.</returns>
        public XElement ToXml()
        {
            return new XElement( DBXmlNames.DB, new XAttribute( DBXmlNames.Version, Version ), Components.Select( c => c.ToXml() ) );
        }

        /// <summary>
        /// Gets the version number of this database.
        /// </summary>
        public long Version { get; }

        /// <summary>
        /// Gets the list of registered components.
        /// </summary>
        public IReadOnlyList<Component> Components { get; }

        /// <summary>
        /// Gets the <see cref="ComponentRef"/> that should be added since they are
        /// currently discovered as embedded inside other ones.
        /// </summary>
        public IEnumerable<ComponentRef> EmbeddedComponents => Components.SelectMany( c => c.Embedded ).Distinct();

        /// <summary>
        /// Tries to finds the best available component for the given runtime, and a 
        /// minimal version.
        /// This method elects the lowest available version first and then the lowest target
        /// framework for this version.
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
        /// Captures the result of <see cref="ComponentDB.AddLocal"/>.
        /// </summary>
        public struct AddLocalResult
        {
            /// <summary>
            /// The new db. Null on error.
            /// </summary>
            public readonly ComponentDB NewDB;
            /// <summary>
            /// The added component. Can be null if the added component
            /// was already registered.
            /// </summary>
            public readonly Component NewComponent;
            /// <summary>
            /// True on error, otherwise false.
            /// </summary>
            public bool Error => NewDB == null;

            internal AddLocalResult( ComponentDB db, Component c = null )
            {
                NewDB = db;
                NewComponent = c;
            }
        }

        /// <summary>
        /// Registers a bin folder. 
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="folder">The folder to register.</param>
        /// <returns>The new ComponentDB with added component.</returns>
        public AddLocalResult AddLocal( IActivityMonitor m, BinFolder folder )
        {
            if( !folder.Heads.Any() )
            {
                m.Error( "No components found." );
                return new AddLocalResult( null );
            }
            var freeHeads = folder.Heads.Where( h => Find( h.ComponentRef ) == null );
            int freeHeadsCount = freeHeads.Count();
            if( freeHeadsCount > 1 )
            {
                m.Error( $"Cannot register '{freeHeads.Select( h => h.Name.Name ).Concatenate( "', '" )}' at the same time. They must be registered individually." );
                return new AddLocalResult( null );
            }
            if( freeHeadsCount == 0 )
            {
                m.Warn( $"No component added (found already registered Components: '{folder.Heads.Select( h => h.Name.Name ).Concatenate( "', '" )}')" );
                return new AddLocalResult( this );
            }
            var toAdd = freeHeads.Single();
            using( m.OpenInfo( $"Found '{toAdd.ComponentRef.EntryPathPrefix}' to register." ) )
            {
                List<ComponentDependency> dependencies = CollectSetupDependencies( m, toAdd.SetupDependencies );

                var embeddedComponents = new List<ComponentRef>();
                IEnumerable<BinFileInfo> binFiles = folder.Files;
                foreach( var sub in folder.Components )
                {
                    if( sub == toAdd ) continue;
                    var cSub = Find( sub.ComponentRef );
                    if( cSub != null )
                    {
                        if( dependencies.Any( d => d.UseName == cSub.Name ) )
                        {
                            m.Error( $"{cSub.Name} is declared as a Setup dependency but exists as an embedded component." );
                            return new AddLocalResult( null );
                        }
                        if( toAdd.StoreFiles && cSub.StoreFiles )
                        {
                            dependencies.Add( new ComponentDependency( cSub.Name, cSub.Version ) );
                        }
                        m.Info( $"Removing {cSub.Files.Count} files thanks to already registered '{cSub.GetRef()}'." );
                        binFiles = binFiles.Where( f => !cSub.Files.Any( fc => fc.Name == f.LocalFileName ) );
                    }
                    else
                    {
                        m.Warn( $"Embedded component '{sub.ComponentRef}' will be included. It should be registered individually." );
                        embeddedComponents.Add( sub.ComponentRef );
                    }
                }
                var files = binFiles.Select( bf => new ComponentFile( bf.LocalFileName, bf.FileLength, bf.ContentSHA1, bf.FileVersion, bf.AssemblyVersion ) );
                var newC = new Component( toAdd.ComponentKind, toAdd.ComponentRef, dependencies, embeddedComponents, files );
                return new AddLocalResult( DoAdd( m, newC ), newC );
            }
        }

        ComponentDB DoAdd( IActivityMonitor m, Component newC )
        {
            return new ComponentDB( this, Components.Select( c => c.WithNewComponent( m, newC ) ).Append( newC ) );
        }

        /// <summary>
        /// Exports a filtered set of components to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="filter">Filter for components to export.</param>
        /// <param name="output">Output stream.</param>
        public void Export( Func<Component, bool> filter, Stream output )
        {
            using( CKBinaryWriter writer = new CKBinaryWriter( output, Encoding.UTF8, true ) )
            {
                // Version is currently 0.
                writer.WriteNonNegativeSmallInt32( 0 );
                foreach( var c in Components )
                {
                    if( filter( c ) )
                    {
                        writer.Write( true );
                        writer.Write( c.ToXml().ToString( SaveOptions.DisableFormatting ) );
                    }
                }
                writer.Write( false );
                writer.Flush();
            }
        }

        public struct ImportResult
        {
            public readonly ComponentDB NewDB;
            /// <summary>
            /// The imported components (whether they ar new or not).
            /// </summary>
            public readonly IReadOnlyList<Component> Components;
            public bool Error => NewDB == null;

            public ImportResult( ComponentDB db, IReadOnlyList<Component> n = null )
            {
                NewDB = db;
                Components = n;
            }
        }

        /// <summary>
        /// Imports a set of components from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="input">Input stream.</param>
        /// <param name="cancellation">Optional cancellation token.</param>
        /// <returns>The new ComponentDB with imported components.</returns>
        public ImportResult Import( IActivityMonitor monitor, Stream input )
        {
            using( monitor.OpenInfo( "Starting components import." ) )
            using( CKBinaryReader reader = new CKBinaryReader( input, Encoding.UTF8, true ) )
            {
                var newOnes = new List<ComponentRef>();
                ComponentDB currentDb = this;
                try
                {
                    var v = reader.ReadNonNegativeSmallInt32();
                    monitor.Debug( $"Stream version: {v}" );
                    while( reader.ReadBoolean() )
                    {
                        var newC = new Component( XElement.Parse( reader.ReadString() ) );
                        bool skip = currentDb.Find( newC.GetRef() ) != null;
                        if( skip )
                        {
                            monitor.Warn( $"Skipping '{newC}' since it already exists." );
                        }
                        else
                        {
                            monitor.Trace( $"Importing Component '{newC}' ({newC.Files.Count} files)." );
                            currentDb = currentDb.DoAdd( monitor, newC );
                         }
                         newOnes.Add( newC.GetRef() );
                    }
                }
                catch( Exception ex )
                {
                    monitor.Error( ex );
                    return new ImportResult( null );
                }
                return new ImportResult( currentDb, newOnes.Select( n => currentDb.Components.Single( c => c.GetRef().Equals( n ) ) ).ToList() );
            }
        }

        /// <summary>
        /// Gets a list of available components.
        /// </summary>
        /// <param name="what">Required description.</param>
        /// <param name="monitor">Optional monitor to use.</param>
        /// <returns>Available components (can be empty).</returns>
        public ISet<Component> FindAvailable( ComponentMissingDescription what, IActivityMonitor monitor = null )
        {
            var result = new HashSet<Component>();
            using( monitor?.OpenInfo( $"Finding available components." ) )
            {
                if( what.Components.Count > 0 )
                {
                    foreach( var cRef in what.Components )
                    {
                        Component c = Components.FirstOrDefault( x => x.GetRef().Equals( cRef ) );
                        if( c.ComponentKind != ComponentKind.None )
                        {
                            result.Add( c );
                        }
                        else monitor?.Warn( $"Component {cRef} not found." );
                    }
                    if( result.Count == 0 )
                    {
                        monitor?.Warn( "No component found." );
                    }
                    else
                    {
                        monitor?.Info( $"Found: {result.Select( c => c.GetRef().ToString() ).Concatenate()}." );
                    }
                }
                if( what.Dependencies.Count > 0 )
                {
                    using( monitor?.OpenInfo( $"Resolving dependencies for {what.TargetRuntime}: {what.Dependencies.Select( d => d.ToString() ).Concatenate()}." ) )
                    {
                        int embeddedCount = result.Count;
                        foreach( var dep in what.Dependencies )
                        {
                            var c = FindBest( what.TargetRuntime, dep.UseName, dep.UseMinVersion );
                            if( c != null )
                            {
                                result.Add( c );
                            }
                            else
                            {
                                monitor?.Warn( $"Unresolved dependency: {dep}" );
                            }
                        }
                        if( result.Count == embeddedCount )
                        {
                            monitor?.Warn( "No dependency resolved." );
                        }
                        else
                        {
                            monitor?.Info( $"Resolved components: {result.Skip(embeddedCount).Select( c => c.GetRef().ToString() ).Concatenate()}." );
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a new <see cref="DependencyResolver"/> for a target <see cref="BinFolder"/>.
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="target">The targets for which required components must be found.</param>
        /// <returns>Null on error, otherwise the DependencyResolver (may be empty).</returns>
        public DependencyResolver GetRuntimeDependenciesResolver( IActivityMonitor m, IEnumerable<BinFolder> targets )
        {
            using( m.OpenInfo( $"Creating runtime dependencies resolver for {targets.Select( t => t.BinPath ).Concatenate()}." ) )
            {
                var models = targets.SelectMany( t => t.Components ).Where( c => c.ComponentKind == ComponentKind.Model );
                if( !models.Any() )
                {
                    m.Warn( "No Model component found." );
                    return new DependencyResolver( this, TargetRuntime.None, Array.Empty<ComponentDependency>() );
                }
                foreach( var eOrR in targets.SelectMany( t => t.Components ).Where( c => c.ComponentKind != ComponentKind.Model ) )
                {
                    m.Warn( $"Found a SetupDependency '{eOrR.ComponentRef}' component. It will be ignored: only Models are considered when selecting TargetRuntime." );
                }
                var targetRuntime = SelectTargetRuntime( m, models );
                if( targetRuntime == TargetRuntime.None ) return null;

                var rootDeps = CollectSetupDependencies( m, models.SelectMany( b => b.SetupDependencies ) );
                if( rootDeps.Count == 0 ) m.Warn( "No Setup Dependency components found." );
                return new DependencyResolver( this, targetRuntime, rootDeps );
            }
        }

        static TargetRuntime SelectTargetRuntime( IActivityMonitor m, IEnumerable<BinFileAssemblyInfo> models )
        {
            using( m.OpenInfo( $"Detecting runtimes for: { models.Select( x => x.Name.Name + '/' + x.ComponentRef.TargetFramework ).Concatenate() }" ) )
            {
                var runtimes = models.First().ComponentRef.TargetFramework.GetCommonRuntimes( models.Skip( 1 ).Select( x => x.ComponentRef.TargetFramework ) );
                if( !runtimes.Any() )
                {
                    m.Error( $"Unable to determine at least one common allowed runtime." );
                    return TargetRuntime.None;
                }
                var theOnlyOne = runtimes.Count() == 1 ? runtimes.First() : TargetRuntime.None;
                if( theOnlyOne != TargetRuntime.None )
                {
                    m.CloseGroup( $"Single selected runtime: {theOnlyOne}." );
                    return theOnlyOne;
                }
                m.Info( $"Multiple possible runtime: {runtimes.Select( r => r.ToString() ).Concatenate()}." );
                theOnlyOne = runtimes.Min();
                if( theOnlyOne == TargetRuntime.NetCoreApp11 ) theOnlyOne = TargetRuntime.NetCoreApp20;
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
                    using( m.OpenWarn( $"Version upgrade for '{name}'. Using: {max}." ) )
                    {
                        foreach( var c in culprits )
                        {
                            m.Warn( $"'{c.Source.Name.Name}' declares to use the version {c.UseMinVersion}." );
                        }
                    }
                    dependencies.Add( new ComponentDependency( name, max ) );
                }
            }
            return dependencies;
        }
    }
}
