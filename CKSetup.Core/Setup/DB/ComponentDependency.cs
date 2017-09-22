using CK.Core;
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
    /// A component dependency is defined by a <see cref="UseName"/> component's name
    /// and its <see cref="UseMinVersion"/> minimal version.
    /// This is an immutable and equatable value (both nme and minimal version must be equal).
    /// </summary>
    public class ComponentDependency : IEquatable<ComponentDependency>
    {
        internal ComponentDependency( string name, SVersion version )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( name ) );
            Debug.Assert( version == null || version.Text != null );
            UseName = name;
            UseMinVersion = version;
        }

        internal ComponentDependency( XElement e )
        {
            UseName = (string)e.AttributeRequired( DBXmlNames.Name );
            var sV = (string)e.Attribute( DBXmlNames.Version );
            if( sV != null ) UseMinVersion = SVersion.Parse( sV );
        }

        internal XElement ToXml()
        {
            return new XElement( DBXmlNames.Dependency, 
                                    new XAttribute( DBXmlNames.Name, UseName ),
                                    UseMinVersion != null 
                                        ? new XAttribute( DBXmlNames.Version, UseMinVersion ) 
                                        : null );
        }

        /// <summary>
        /// Gets the name of the referenced component.
        /// </summary>
        public string UseName { get; }

        /// <summary>
        /// Gets the minimal version. Can be null.
        /// </summary>
        public SVersion UseMinVersion { get; }

        public override string ToString() => $"-> {UseName}/{(UseMinVersion == null ? "<no min version>" : UseMinVersion.Text)}";

        public bool Equals( ComponentDependency other ) => other != null && other.UseName == UseName && other.UseMinVersion == UseMinVersion;

        public override int GetHashCode() => UseMinVersion.GetHashCode() ^ UseName.GetHashCode();

        public override bool Equals( object obj ) => Equals( obj as ComponentDependency );

    }
}
