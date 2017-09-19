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
    public class ComponentFile : IEquatable<ComponentFile>
    {
        /// <summary>
        /// Initializes a new <see cref="ComponentFile"/>.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <param name="length">Length. Must be positive.</param>
        /// <param name="sha1">The SHA1 of the file.</param>
        /// <param name="fileVersion">The FileVersion from the VERSIONINFO file header if it exists.</param>
        /// <param name="assemblyVersion">The assembly version if it exists.</param>
        public ComponentFile( string name, int length, SHA1Value sha1, Version fileVersion, Version assemblyVersion )
        {
            if( string.IsNullOrWhiteSpace( name ) ) throw new ArgumentNullException( nameof( name ) );
            if( length <= 0 ) throw new ArgumentOutOfRangeException( nameof( length ) );
            Name = name;
            Length = length;
            SHA1 = sha1;
            FileVersion = fileVersion;
            AssemblyVersion = assemblyVersion;
        }

        public string Name { get; }

        public int Length { get; }

        public SHA1Value SHA1 { get; }

        /// <summary>
        /// Gets the file version from the <see cref="System.Diagnostics.FileVersionInfo"/> if the file has a PE header with a VERSIONINFO.
        /// Null otherwise.
        /// </summary>
        public Version FileVersion { get; }

        /// <summary>
        /// Gets the assembly version if it exists. Null otherwise.
        /// </summary>
        public Version AssemblyVersion { get; }

        public ComponentFile( XElement e )
        {
            Name = (string)e.Attribute( DBXmlNames.Name );
            Length = (int)e.Attribute( DBXmlNames.Length );
            SHA1 = SHA1Value.Parse( (string)e.Attribute( DBXmlNames.SHA1 ) );
            string v = (string)e.Attribute( DBXmlNames.FileVersion );
            FileVersion = v != null ? new Version( v ) : null;
            v = (string)e.Attribute( DBXmlNames.AssemblyVersion );
            AssemblyVersion = v != null ? new Version( v ) : null;
            CheckValid();
        }

        public bool Equals( ComponentFile other ) => SHA1 == other?.SHA1;

        public override bool Equals( object obj ) => Equals( obj as ComponentFile );

        public override int GetHashCode() => SHA1.GetHashCode();

        public XElement ToXml()
        {
            return new XElement( DBXmlNames.File,
                                    new XAttribute( DBXmlNames.Name, Name ),
                                    new XAttribute( DBXmlNames.Length, Length ),
                                    new XAttribute( DBXmlNames.SHA1, SHA1.ToString() ),
                                    FileVersion != null ? new XAttribute( DBXmlNames.FileVersion, FileVersion.ToString() ) : null,
                                    AssemblyVersion != null ? new XAttribute( DBXmlNames.AssemblyVersion, AssemblyVersion.ToString() ) : null
                                );
        }

        void CheckValid()
        {
            if( string.IsNullOrWhiteSpace( Name ) ) throw new ArgumentNullException( nameof( Name ) );
            if( Length <= 0 ) throw new ArgumentOutOfRangeException( nameof( Length ) );
        }

        public override string ToString()
        {
            return $"{Name} ({Length}), fV: {FileVersion} aV: {AssemblyVersion}, sha1: {SHA1}";
        }

    }
}
