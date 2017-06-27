using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CK.Core;

namespace CKSetup
{
    public class Component
    {
        readonly ComponentRef _ref;

        public Component(
            ComponentKind k, 
            ComponentRef cRef,
            IEnumerable<ComponentDependency> dependencies,
            IEnumerable<ComponentRef> embedded,
            IEnumerable<string> files)
        {
            _ref = cRef;
            ComponentKind = k;
            Dependencies = dependencies.ToArray();
            Embedded = embedded.ToArray();
            Files = files.ToArray();
            CheckValid();
        }

        public Component( XElement e, Func<ComponentRef,Component> find )
        {
            _ref = new ComponentRef( e );
            ComponentKind = e.AttributeEnum( DBXmlNames.Kind, ComponentKind.None );
            Dependencies = e.Elements( DBXmlNames.Dependencies )
                                .Elements( DBXmlNames.Dependency )
                                .Select( d => new ComponentDependency( d ) ).ToArray();
            Embedded = e.Elements( DBXmlNames.EmbeddedComponents )
                                .Elements( DBXmlNames.Ref )
                                .Select( d => new ComponentRef( d ) ).ToArray();
            Files = e.Elements( DBXmlNames.Files )
                                .Elements( DBXmlNames.File )
                                .Select( f => f.Value ).ToArray();
            CheckValid();
        }

        private void CheckValid()
        {
            if( ComponentKind == ComponentKind.None ) throw new ArgumentException( "Invalid ComponentKind." );
            if( Dependencies.Contains( null ) ) throw new ArgumentException( "A dependency can not ne null." );
            if( Files.Contains( null ) ) throw new ArgumentException( "A file can not be null." );
        }

        public XElement ToXml()
        {
            return new XElement( DBXmlNames.Component,
                                    new XAttribute( DBXmlNames.Kind, ComponentKind ),
                                    _ref.XmlContent(),
                                    new XElement( DBXmlNames.Dependencies, Dependencies.Select( c => c.ToXml() ) ),
                                    new XElement( DBXmlNames.EmbeddedComponents, Embedded.Select( c => c.ToXml() ) ),
                                    new XElement( DBXmlNames.Files, Files.Select( f => new XElement( DBXmlNames.File, f ) ) ) );
        }

        public ComponentKind ComponentKind { get; }

        public TargetFramework TargetFramework => _ref.TargetFramework;

        public SVersion Version => _ref.Version;

        public string Name => _ref.Name;

        public bool Is( ComponentRef r ) => _ref.Equals( r );

        public ComponentRef GetRef() => _ref;

        public IReadOnlyList<ComponentDependency> Dependencies { get; }

        public IReadOnlyList<ComponentRef> Embedded { get; }

        public IReadOnlyList<string> Files { get; }

        public Component WithNewComponent( IComponentDBEventSink sink, IActivityMonitor m, Component newC )
        {
            var uselessEmbedded = Embedded.Where( e => e.Equals( newC.GetRef() ) ).SingleOrDefault();
            if( uselessEmbedded.Name == null ) return this;
            var newEmbedded = Embedded.Where( e => !e.Equals( uselessEmbedded ) );
            var newDependecies = Dependencies.Append( new ComponentDependency( newC.Name, newC.Version ) );
            var newFiles = Files.Where( f => !newC.Files.Contains( f ) ).ToList();
            int delta = Files.Count - newFiles.Count;
            if( delta > 0 )
            {
                m.Info().Send( $"Removing {delta} files from '{_ref}' thanks to newly registered '{newC.Name}'." );
            }
            sink?.FilesRemoved( this, newC.Files );
            m.Info().Send( $"Component '{_ref}' now depends on '{newC.GetRef()}' instead of embedding it." );
            return new Component( ComponentKind, _ref, newDependecies, newEmbedded, newFiles );
        }

    }
}
