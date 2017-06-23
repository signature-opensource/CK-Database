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
            IEnumerable<ComponenDependency> dependencies,
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
            ComponentKind = e.AttributeEnum( XmlNames.nKind, ComponentKind.None );
            Dependencies = e.Elements( XmlNames.nDependencies )
                                .Elements( XmlNames.nDependency )
                                .Select( d => new ComponenDependency( d ) ).ToArray();
            Embedded = e.Elements( XmlNames.nEmbeddedComponents )
                                .Elements( XmlNames.nRef )
                                .Select( d => new ComponentRef( d ) ).ToArray();
            Files = e.Elements( XmlNames.nFiles )
                                .Elements( XmlNames.nFile )
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
            return new XElement( XmlNames.nComponent,
                                    new XAttribute( XmlNames.nKind, ComponentKind ),
                                    _ref.XmlContent(),
                                    new XElement( XmlNames.nDependencies, Dependencies.Select( c => c.ToXml() ) ),
                                    new XElement( XmlNames.nEmbeddedComponents, Embedded.Select( c => c.ToXml() ) ),
                                    new XElement( XmlNames.nFiles, Files.Select( f => new XElement( XmlNames.nFile, f ) ) ) );
        }

        public ComponentKind ComponentKind { get; }

        public TargetFramework TargetFramework => _ref.TargetFramework;

        public SVersion Version => _ref.Version;

        public string Name => _ref.Name;

        public bool Is( ComponentRef r ) => _ref.Equals( r );

        public ComponentRef GetRef() => _ref;

        public IReadOnlyList<ComponenDependency> Dependencies { get; }

        public IReadOnlyList<ComponentRef> Embedded { get; }

        public IReadOnlyList<string> Files { get; }

        public Component WithNewComponent( IComponentDBEventSink sink, IActivityMonitor m, Component newC )
        {
            var uselessEmbedded = Embedded.Where( e => e.Equals( newC.GetRef() ) ).SingleOrDefault();
            if( uselessEmbedded.Name == null ) return this;
            var newEmbedded = Embedded.Where( e => !e.Equals( uselessEmbedded ) );
            var newDependecies = Dependencies.Append( new ComponenDependency( newC.Name, newC.Version ) );
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
