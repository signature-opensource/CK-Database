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
        readonly CSVersion _version;

        public ComponentRef( TargetFramework t, string n, CSVersion v )
        {
            _targetFramework = t;
            _name = n;
            _version = v;
            CheckValid();
        }

        public ComponentRef( XElement e )
        {
            _targetFramework = e.AttributeEnum( nTargetFramework, TargetFramework.None );
            _name = (string)e.AttributeRequired( nName );
            _version = CSVersion.TryParse( (string)e.AttributeRequired( nVersion ) );
            CheckValid();
        }

        void CheckValid()
        {
            if( _targetFramework == TargetFramework.None ) throw new ArgumentException( "Invalid TargetFramework." );
            if( string.IsNullOrWhiteSpace( _name ) ) throw new ArgumentException( "Invalid Name." );
            if( _version == null ) throw new ArgumentException( "Invalid Version." );
        }

        internal static readonly XName nRef = XNamespace.None + "Ref";
        static readonly XName nTargetFramework = XNamespace.None + "TargetFramework";
        static readonly XName nName = XNamespace.None + "Name";
        static readonly XName nVersion = XNamespace.None + "Version";

        public TargetFramework TargetFramework => _targetFramework;

        public string Name => _name;

        public ComponentRef WithTargetFramework( TargetFramework t ) => new ComponentRef( t, _name, _version );

        public CSVersion Version => _version;

        public XElement ToXml() => new XElement( nRef, XmlContent() );

        internal IEnumerable<XObject> XmlContent()
        {
            yield return new XAttribute( nTargetFramework, _targetFramework );
            yield return new XAttribute( nName, _name );
            yield return new XAttribute( nVersion, _version.ToString( CSVersionFormat.SemVer ) );
        }

        public override string ToString() => $"{TargetFramework}/{Name}/{Version.ToString( CSVersionFormat.SemVer )}";

        public bool Equals( ComponentRef other ) => _targetFramework == other._targetFramework && _name == other._name && _version == other._version;

        public override bool Equals( object obj ) => obj is ComponentRef ? Equals( (ComponentRef)obj ) : false;

        public override int GetHashCode() => Util.Hash.Combine( (long)_targetFramework, _name, _version ).GetHashCode();
    }
}
