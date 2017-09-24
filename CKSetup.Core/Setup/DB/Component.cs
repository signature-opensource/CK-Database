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
    /// <summary>
    /// Immputable component description.
    /// </summary>
    public class Component
    {
        readonly ComponentRef _ref;


        /// <summary>
        /// An empty, none, null component object.
        /// </summary>
        public static Component None = new Component();

        Component()
        {
            _ref = new ComponentRef( String.Empty, TargetFramework.None, SVersion.ZeroVersion );
            ComponentKind = ComponentKind.None;
            Dependencies = Array.Empty<ComponentDependency>();
            Embedded = Array.Empty<ComponentRef>();
            Files = Array.Empty<ComponentFile>();
        }

        public Component(
            ComponentKind k, 
            ComponentRef cRef,
            IReadOnlyList<ComponentDependency> dependencies,
            IEnumerable<ComponentRef> embedded,
            IEnumerable<ComponentFile> files)
        {
            _ref = cRef;
            ComponentKind = k;
            Dependencies = dependencies;
            Embedded = embedded.ToArray();
            Files = files.ToArray();
            CheckValid();
        }

        public Component( XElement e )
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
                                .Select( f => new ComponentFile( f ) ).ToArray();
            CheckValid();
        }

        void CheckValid()
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
                                    new XElement( DBXmlNames.Files, Files.Select( f => f.ToXml() ) ) );
        }

        public ComponentKind ComponentKind { get; }

        /// <summary>
        /// Gets whether files should be stored: only SetupDependency need to be stored.
        /// </summary>
        public bool StoreFiles => ComponentKind == ComponentKind.SetupDependency;

        public TargetFramework TargetFramework => _ref.TargetFramework;

        public SVersion Version => _ref.Version;

        public string Name => _ref.Name;

        public bool Is( ComponentRef r ) => _ref.Equals( r );

        public ComponentRef GetRef() => _ref;

        public IReadOnlyList<ComponentDependency> Dependencies { get; }

        public IReadOnlyList<ComponentRef> Embedded { get; }

        public IReadOnlyList<ComponentFile> Files { get; }

        /// <summary>
        /// Overridden to return this <see cref="ComponentRef.EntryPathPrefix"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => _ref.ToString();

        public Component WithNewComponent( IActivityMonitor m, Component newC )
        {
            var uselessEmbedded = Embedded.Where( e => e.Equals( newC.GetRef() ) ).SingleOrDefault();
            if( uselessEmbedded.Name == null ) return this;

            var newEmbedded = Embedded.Where( e => !e.Equals( uselessEmbedded ) );
            var newDependencies = StoreFiles && newC.StoreFiles
                                    ? Dependencies.Append( new ComponentDependency( uselessEmbedded.Name, uselessEmbedded.Version ) ).ToList()
                                    : Dependencies;
            var newFiles = Files.Where( f => !newC.Files.Any( cf => cf.Name == f.Name ) ).ToList();
            int delta = Files.Count - newFiles.Count;
            if( delta > 0 )
            {
                m.Info( $"Removing {delta} files from '{_ref}' thanks to newly registered '{newC.Name}'." );
            }
            m.Info( $"Component '{_ref}' does not embedd '{newC.GetRef()}' anymore." );
            return new Component( ComponentKind, _ref, newDependencies, newEmbedded, newFiles );
        }

    }
}
