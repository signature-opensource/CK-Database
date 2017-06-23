using CK.Core;
using CK.Text;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            foreach( var c in e.Elements( XmlNames.nComponent ) )
            {
                comps.Add( new Component( c, FindRequired ) );
            }
        }

        public XElement ToXml()
        {
            return new XElement( XmlNames.nDB, Components.Select( c => c.ToXml() ) );
        }

        public IReadOnlyList<Component> Components { get; }

        public Component Find( ComponentRef r ) => Components.FirstOrDefault( c => c.Is( r ) );

        public Component FindRequired( ComponentRef r )
        {
            Component c = Find( r );
            if( c == null ) throw new InvalidOperationException( $"Component '{r}' is not registered." );
            return c;
        }

        //public Component FindMaxVersion( TargetFramework t, string name ) => Components.Where( c => c.TargetFramework == t && c.Name == name ).OrderByDescending( c => c.Version.OrderedVersion ).FirstOrDefault();

        //Component FindDependency( IActivityMonitor m, TargetFramework t, string name, CSVersion v, bool required )
        //{
        //    Debug.Assert( t != TargetFramework.None );
        //    Debug.Assert( !string.IsNullOrWhiteSpace( name ) );
        //    Component found;
        //    if( v != null )
        //    {
        //        var cRef = new ComponentRef( t, name, v );
        //        found = Components.FirstOrDefault( c => c.Is( cRef ) );
        //        if( found != null ) return found;
        //        if( t == TargetFramework.NetStandard16 )
        //        {
        //            found = Components.FirstOrDefault( c => c.Is( cRef.WithTargetFramework( TargetFramework.NetStandard13 ) ) );
        //            if( found != null ) return found;
        //        }
        //        if( required ) m.Error().Send( $"Unable to find the dependency '{cRef}'. It must be registered first." );
        //        return null;
        //    }
        //    found = FindMaxVersion( t, name );
        //    if( found != null ) return found;
        //    if( t == TargetFramework.NetStandard16 )
        //    {
        //        found = FindMaxVersion( TargetFramework.NetStandard13, name );
        //        if( found != null ) return found;
        //    }
        //    if( required ) m.Error().Send( $"Unable to find the dependency '{name}'. It must be registered first." );
        //    return null;
        //}

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

            List<ComponenDependency> dependencies = CollectSetupDependencies( m, new[] { toAdd } );

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
                    dependencies.Add( new ComponenDependency( cSub.Name, cSub.Version ) );
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

        static List<ComponenDependency> CollectSetupDependencies( IActivityMonitor m, IEnumerable<BinFileInfo> starts )
        {
            var dependencies = new List<ComponenDependency>();
            IEnumerable<SetupDependency> gDep = starts.SelectMany( b => b.SetupDependencies );

            foreach( var dep in gDep.GroupBy( d => d.UseName ) )
            {
                string name = dep.Key;
                var versions = dep.Where( d => d.UseMinVersion != null ).Select( d => d.UseMinVersion ).Distinct().ToList();
                if( versions.Count == 0 )
                {
                    dependencies.Add( new ComponenDependency( name, null ) );
                }
                else if( versions.Count == 1 )
                {
                    dependencies.Add( new ComponenDependency( name, versions[0] ) );
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
                    dependencies.Add( new ComponenDependency( name, max ) );
                }
            }
            return dependencies;
        }
    }
}
