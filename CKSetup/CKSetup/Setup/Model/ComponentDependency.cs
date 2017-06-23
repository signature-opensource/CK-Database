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
    public class ComponenDependency
    {
        internal ComponenDependency( string name, SVersion version )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( name ) );
            UseName = name;
            UseMinVersion = version;
        }

        public ComponenDependency( XElement e )
        {
            UseName = (string)e.AttributeRequired( XmlNames.nName );
            var sV = (string)e.Attribute( XmlNames.nVersion );
            if( sV != null ) UseMinVersion = SVersion.Parse( sV );
        }

        public XElement ToXml()
        {
            return new XElement( XmlNames.nDependency, 
                                    new XAttribute( XmlNames.nName, UseName ),
                                    UseMinVersion != null ? new XAttribute( XmlNames.nVersion, UseMinVersion ) : null );
        }

        public string UseName { get; }

        /// <summary>
        /// Gets the minimal version. Can be null.
        /// </summary>
        public SVersion UseMinVersion { get; }
    }
}
