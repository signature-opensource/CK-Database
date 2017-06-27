using CK.Core;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    public struct ComponentRef : IEquatable<ComponentRef>
    {
        readonly TargetFramework _targetFramework;
        readonly string _name;
        readonly SVersion _version;

        public ComponentRef( TargetFramework t, string n, SVersion v )
        {
            _targetFramework = t;
            _name = n;
            _version = v;
            CheckValid();
        }

        public ComponentRef( XElement e )
        {
            _targetFramework = e.AttributeEnum( DBXmlNames.TargetFramework, TargetFramework.None );
            _name = (string)e.AttributeRequired( DBXmlNames.Name );
            _version = SVersion.Parse( (string)e.AttributeRequired( DBXmlNames.Version ) );
            CheckValid();
        }

        void CheckValid()
        {
            if( _targetFramework == TargetFramework.None ) throw new ArgumentException( "Invalid TargetFramework." );
            if( string.IsNullOrWhiteSpace( _name ) ) throw new ArgumentException( "Invalid Name." );
            if( _version == null || !_version.IsValidSyntax ) throw new ArgumentException( "Invalid Version." );
        }

        public TargetFramework TargetFramework => _targetFramework;

        public string Name => _name;

        public ComponentRef WithTargetFramework( TargetFramework t ) => new ComponentRef( t, _name, _version );

        public SVersion Version => _version;

        public XElement ToXml() => new XElement( DBXmlNames.Ref, XmlContent() );

        internal IEnumerable<XObject> XmlContent()
        {
            yield return new XAttribute( DBXmlNames.TargetFramework, _targetFramework );
            yield return new XAttribute( DBXmlNames.Name, _name );
            yield return new XAttribute( DBXmlNames.Version, _version.Text );
        }

        /// <summary>
        /// Gets the entry path prefix (ends with a /).
        /// </summary>
        public string EntryPathPrefix => $"{Name}/{Version.Text}/{TargetFramework}/";

        public override string ToString() => EntryPathPrefix;

        public bool Equals( ComponentRef other ) => _targetFramework == other._targetFramework && _name == other._name && _version == other._version;

        public override bool Equals( object obj ) => obj is ComponentRef ? Equals( (ComponentRef)obj ) : false;

        public override int GetHashCode() => Util.Hash.Combine( (long)_targetFramework, _name, _version ).GetHashCode();
    }
}
