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
    public class ComponentDependency
    {
        internal ComponentDependency( string name, SVersion version )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( name ) );
            UseName = name;
            UseMinVersion = version;
        }

        public ComponentDependency( XElement e )
        {
            UseName = (string)e.AttributeRequired( DBXmlNames.Name );
            var sV = (string)e.Attribute( DBXmlNames.Version );
            if( sV != null ) UseMinVersion = SVersion.Parse( sV );
        }

        public XElement ToXml()
        {
            return new XElement( DBXmlNames.Dependency, 
                                    new XAttribute( DBXmlNames.Name, UseName ),
                                    UseMinVersion != null ? new XAttribute( DBXmlNames.Version, UseMinVersion ) : null );
        }

        public string UseName { get; }

        /// <summary>
        /// Gets the minimal version. Can be null.
        /// </summary>
        public SVersion UseMinVersion { get; }

        public override string ToString() => $"-> {UseName}/{UseMinVersion.Text}";
    }
}
