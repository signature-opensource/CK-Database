using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CK.Setup.Database.SqlServer
{
    public class SqlObject : ISetupableItem
    {
        string _fullName;
        IDependentItemContainerRef _container;
        IEnumerable<string> _requires;
        IEnumerable<string> _requiredBy;
        Version _version;
        IEnumerable<VersionedName> _previousNames;
        string _header;
        string _type;
        string _textAfterName;

        internal SqlObject( SetupableItemData d, SqlServerObjectPreParsed pre )
        {
            Debug.Assert( d.FullName == pre.FullName );
            _fullName = d.FullName;
            _container = d.Container;
            _requires = d.Requires;
            _requiredBy = d.RequiredBy;
            _version = d.Version;
            _previousNames = d.PreviousNames;
            
            _header = pre.Header;
            _type = pre.Type;
            _textAfterName = pre.Text.Substring( pre.Match.Index + pre.Match.Length );
        }

        public string FullName
        {
            get { return _fullName; }
        }

        public IEnumerable<string> Requires
        {
            get { return _requires; }
        }

        public IEnumerable<string> RequiredBy
        {
            get { return _requiredBy; }
        }

        public Version Version
        {
            get { return _version; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _previousNames; }
        }

        string IVersionedItem.ItemType
        {
            get { return _type; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _container; }
        }

        public string SetupDriverTypeName 
        { 
            get { return typeof(SqlObjectDriver).AssemblyQualifiedName; } 
        }

        /// <summary>
        /// Writes the drop instruction.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteDrop( TextWriter b )
        {
            b.Write( "if OBJECT_ID('" );
            b.Write( FullName );
            b.Write( "') is not null drop " );
            b.Write( _type );
            b.Write( ' ' );
            b.Write( FullName );
            b.WriteLine( ';' );
        }

        /// <summary>
        /// Writes the whole object.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteCreate( TextWriter b )
        {
            b.WriteLine( _header );
            b.Write( "create " );
            b.Write( _type );
            b.Write( ' ' );
            b.Write( _fullName );
            b.WriteLine( _textAfterName );
        }


    }
}
