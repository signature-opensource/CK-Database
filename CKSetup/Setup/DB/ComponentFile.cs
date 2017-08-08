using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup
{
    /// <summary>
    /// Immutable file description.
    /// </summary>
    public class ComponentFile
    {
        /// <summary>
        /// Initializes a new <see cref="ComponentFile"/>.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <param name="length">Length. Must be positive.</param>
        /// <param name="sha1">The SHA1 of the file.</param>
        public ComponentFile( string name, int length, SHA1Value sha1 )
        {
            if( string.IsNullOrWhiteSpace( name ) ) throw new ArgumentNullException( nameof( name ) );
            if( length <= 0 ) throw new ArgumentOutOfRangeException( nameof( length ) );
            Name = name;
            Length = length;
            SHA1 = sha1;
        }

        public string Name { get; }

        public int Length { get; }

        public SHA1Value SHA1 { get; }

        public ComponentFile( XElement e )
        {
            Name = (string)e.Attribute( DBXmlNames.Name );
            Length = (int)e.Attribute( DBXmlNames.Length );
            SHA1 = SHA1Value.Parse( (string)e.Attribute( DBXmlNames.SHA1 ) );
            CheckValid();
        }

        public XElement ToXml()
        {
            return new XElement( DBXmlNames.File,
                                    new XAttribute( DBXmlNames.Name, Name ),
                                    new XAttribute( DBXmlNames.Length, Length ),
                                    new XAttribute( DBXmlNames.SHA1, SHA1.ToString() )
                                    );
        }

        void CheckValid()
        {
            if( string.IsNullOrWhiteSpace( Name ) ) throw new ArgumentNullException( nameof( Name ) );
            if( Length <= 0 ) throw new ArgumentOutOfRangeException( nameof( Length ) );
        }


    }
}
