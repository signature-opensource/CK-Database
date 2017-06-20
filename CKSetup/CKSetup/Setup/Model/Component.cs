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
            IEnumerable<Component> dependencies,
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

        static internal readonly XName nComponent = XNamespace.None + "Component";
        static readonly XName nKind = XNamespace.None + "Kind";
        static readonly XName nDependencies = XNamespace.None + "Dependencies";
        static readonly XName nEmbeddedComponents = XNamespace.None + "EmbeddedComponents";
        static readonly XName nFiles = XNamespace.None + "Files";
        static readonly XName nFile = XNamespace.None + "File";

        public Component( XElement e, Func<ComponentRef,Component> find )
        {
            _ref = new ComponentRef( e );
            ComponentKind = e.AttributeEnum( nKind, ComponentKind.None );
            Dependencies = e.Elements( nDependencies )
                                .Elements( ComponentRef.nRef )
                                .Select( d => find( new ComponentRef( d ) ) ).ToArray();
            Embedded = e.Elements( nEmbeddedComponents ).Select( d => new ComponentRef( d ) ).ToArray();
            Files = e.Elements( nFiles ).Elements( nFile ).Select( f => f.Value ).ToArray();
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
            return new XElement( nComponent,
                                    new XAttribute( nKind, ComponentKind ),
                                    _ref.XmlContent(),
                                    new XElement( nDependencies, Dependencies.Select( c => c.GetRef().ToXml() ) ),
                                    new XElement( nEmbeddedComponents, Embedded.Select( c => c.ToXml() ) ),
                                    new XElement( nFiles, Files.Select( f => new XElement( nFile, f ) ) ) );
        }

        public ComponentKind ComponentKind { get; }

        public TargetFramework TargetFramework { get; }

        public CSVersion Version { get; }

        public string Name { get; }

        public bool Is( ComponentRef r ) => _ref.Equals( r );

        public ComponentRef GetRef() => _ref;

        public IReadOnlyList<Component> Dependencies { get; }

        public IReadOnlyList<ComponentRef> Embedded { get; }

        public IReadOnlyList<string> Files { get; }

        public Component WithNewComponent( IActivityMonitor m, Component newC )
        {
            var uselessSub = Embedded.Where( e => e.Equals( newC.GetRef() ) ).SingleOrDefault();
            if( uselessSub.Name == null ) return this;
            var newEmbedded = Embedded.Where( e => !e.Equals( uselessSub ) ).ToArray();
            var newDependecies = Dependencies.Append( newC ).ToArray();
            var newFiles = Files.Where( f => !newC.Files.Contains( f ) );
            m.Info().Send( $"Component '{_ref}' now depends on '{newC.GetRef()}' instead of embedding it." );
            return new Component( ComponentKind, _ref, newDependecies, newEmbedded, newFiles );
        }

    }
}
