using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace CKSetup
{
    /// <summary>
    /// Immutable collection of <see cref="Component"/>.
    /// </summary>
    public class ComponentDB
    {
        readonly IComponentDBEventSink _sink;

        public ComponentDB( IComponentDBEventSink c )
        {
            _sink = c;
            Components = Array.Empty<Component>();
        }

        ComponentDB( IComponentDBEventSink sink, IEnumerable<Component> components )
        {
            _sink = sink;
            Components = components.ToArray();
        }

        public ComponentDB( IComponentDBEventSink sink, XElement e )
        {
            _sink = sink;
            var comps = new List<Component>();
            Components = comps;
            foreach( var c in e.Elements( DBXmlNames.Component ) )
            {
                comps.Add( new Component( c, FindRequired ) );
            }
        }

        public XElement ToXml()
        {
            return new XElement( DBXmlNames.DB, Components.Select( c => c.ToXml() ) );
        }

        public IReadOnlyList<Component> Components { get; }

        public Component Find( ComponentRef r ) => Components.FirstOrDefault( c => c.Is( r ) );

        public Component FindRequired( ComponentRef r )
        {
            Component c = Find( r );
            if( c == null ) throw new InvalidOperationException( $"Component '{r}' is not registered." );
            return c;
        }

        public ComponentDB Add( IActivityMonitor m, BinFolder folder )
        {
            if( !folder.Heads.Any() ) throw new ArgumentException();
            var freeHeads = folder.Heads.Where( h => Find( h.ComponentRef ) == null );
            int freeHeadsCount = freeHeads.Count();
            if( freeHeadsCount > 1 )
            {
                m.Error().Send( $"Cannot register '{freeHeads.Select( h => h.Name.Name ).Concatenate( "', '" )}' at the same time. They must be registered individually." );
                return null;
            }
            if( freeHeadsCount == 0 )
            {
                m.Warn().Send( $"Components '{folder.Heads.Select( h => h.Name.Name ).Concatenate( "', '" )}' are already registered." );
                return this;
            }
            BinFileInfo toAdd = freeHeads.Single();

            List<ComponentDependency> dependencies = CollectSetupDependencies( m, new[] { toAdd } );

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
                    dependencies.Add( new ComponentDependency( cSub.Name, cSub.Version ) );
                    m.Info().Send( $"Removing {cSub.Files.Count} files thanks to already registered '{cSub.GetRef()}'." );
                    files = files.Where( f => !cSub.Files.Contains( f ) );
                }
                else
                {
                    m.Warn().Send( $"Sub component '{sub.ComponentRef}' will be included. It should be registered individually." );
                    embeddedComponents.Add( sub.ComponentRef );
                }
            }
            var newC = new Component( toAdd.ComponentKind, toAdd.ComponentRef, dependencies, embeddedComponents, files );
            _sink?.ComponentAdded( newC, folder );
            return new ComponentDB( _sink, Components.Select( c => c.WithNewComponent( _sink, m, newC ) ).Append( newC ) );
        }

        /// <summary>
        /// Resolves required components for a target <see cref="BinFolder"/>.
        /// </summary>
        /// <param name="m">The monitor to use.</param>
        /// <param name="target">The target for which required components must be found.</param>
        /// <returns>Null on error, otherwise the components to install (may be empty).</returns>
        public IReadOnlyList<Component> ResolveRuntimeDependencies( IActivityMonitor m, BinFolder target )
        {
            using( m.OpenInfo().Send( $"Resolving runtime dependencies for {target.BinPath}." ) )
            {
                var models = target.Components.Where( c => c.ComponentKind == ComponentKind.Model );
                if( !models.Any() )
                {
                    m.Warn().Send( "No Model component found." );
                    return Array.Empty<Component>();
                }
                foreach( var eOrR in target.Components.Where( c => c.ComponentKind != ComponentKind.Model ) )
                {
                    m.Warn().Send( $"{eOrR.ComponentKind} '{eOrR.ComponentRef}' found. It will be ignored." );
                }
                var targetFramework = GetSingleTargetFramework( m, models );
                if( targetFramework == TargetFramework.None ) return null;

                List<ComponentDependency> rootDeps = CollectSetupDependencies( m, models );

                var installableComponents = GetInstallableComponents( m, rootDeps, targetFramework );
                if( installableComponents == null ) return null;

                var toInstall = installableComponents.ResolveMinimalVersions();
                m.CloseGroup( toInstall.Count == 0 ? "No component found." : $"{toInstall.Count} components fond." );
                return toInstall;
            }
        }

        class InstallableComponents
        {
            readonly ComponentDB _db;
            readonly TargetFramework _targetFramework;
            readonly List<Component> _components;
            readonly int _initialCount;

            public InstallableComponents( ComponentDB db, TargetFramework targetFramework, IEnumerable<Component> c )
            {
                _db = db;
                _targetFramework = targetFramework;
                _components = c.ToList();
                _initialCount = _components.Count;
            }

            public List<Component> ResolveMinimalVersions()
            {
                int idx = 0;
                do
                {
                    foreach( var dep in _components[idx].Dependencies )
                    {
                        int alreadyHereIdx = _components.IndexOf( c => c.Name == dep.UseName );
                        Debug.Assert( _db.Components.Single( c => c.Name == dep.UseName && c.TargetFramework == _targetFramework && c.Version == dep.UseMinVersion ) != null );
                        if( alreadyHereIdx < 0 )
                        {
                            // This is a new Component.
                            _components.Add( _db.Components.First( c => c.Name == dep.UseName && c.TargetFramework == _targetFramework && c.Version == dep.UseMinVersion ) );
                        }
                        else 
                        {
                            // If the required version is lower or equal to the already choosen component,
                            // we have nothing to do.
                            if( dep.UseMinVersion > _components[alreadyHereIdx].Version )
                            {
                                // We always update the list with the new (greater) version of the component.
                                _components[idx] = _db.Components.First( c => c.Name == dep.UseName && c.TargetFramework == _targetFramework && c.Version == dep.UseMinVersion );
                                // Did we already processed the depependencies of this component?
                                if( alreadyHereIdx > idx )
                                {
                                    // Yes: we have to restart the whole process.
                                    // We keep at least our initial count or, if we made progress,
                                    // the current successfuly processed set.
                                    int succesfulIndex = Math.Max( idx+1, _initialCount );
                                    var r = new InstallableComponents( _db, _targetFramework, _components.Take( succesfulIndex ) );
                                    return r.ResolveMinimalVersions();
                                }
                            }
                        }
                    }
                }
                while( ++idx < _components.Count );
                return _components;
            }
        }

        private InstallableComponents GetInstallableComponents( IActivityMonitor m, IEnumerable<ComponentDependency> rootDeps, TargetFramework targetFramework )
        {
            var requiredComponents = rootDeps.Select( d => new
            {
                Root = d,
                Component = Components
                                .Where( c => c.TargetFramework == targetFramework && c.Name == d.UseName && c.Version >= d.UseMinVersion )
                                .OrderByDescending( c => c.Version )
                                .FirstOrDefault()
            } );
            var missing = requiredComponents.Where( r => r.Component == null );
            if( missing.Any() )
            {
                foreach( var missed in missing )
                {
                    var c = missed.Root;
                    if( c.UseMinVersion != null )
                        m.Error().Send( $"Unable to find component {c.UseName}/{targetFramework} with version at least {c.UseMinVersion}." );
                    else m.Error().Send( $"Unable to find component {c.UseName}/{targetFramework} (any version)." );
                    return null;
                }
            }
            return new InstallableComponents( this, targetFramework, requiredComponents.Select( r => r.Component ) );
        }

        static TargetFramework GetSingleTargetFramework( IActivityMonitor m, IEnumerable<BinFileInfo> models )
        {
            var random = models.First();
            var targetFramework = random.ComponentRef.TargetFramework;
            m.Info().Send( $"Using framework {targetFramework} based on '{random.LocalFileName}'." );
            var conflicts = models.Where( model => model.ComponentRef.TargetFramework != targetFramework );
            if( conflicts.Any() )
            {
                foreach( var conflict in conflicts )
                {
                    m.Error().Send( $"Model {conflict.ComponentRef} does not target the framework {targetFramework}." );
                }
                return TargetFramework.None;
            }
            return targetFramework;
        }

        static List<ComponentDependency> CollectSetupDependencies( IActivityMonitor m, IEnumerable<BinFileInfo> starts )
        {
            var dependencies = new List<ComponentDependency>();
            IEnumerable<SetupDependency> gDep = starts.SelectMany( b => b.SetupDependencies );

            foreach( var dep in gDep.GroupBy( d => d.UseName ) )
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
