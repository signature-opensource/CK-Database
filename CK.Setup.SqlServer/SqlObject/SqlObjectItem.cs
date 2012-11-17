using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlObjectItem : IVersionedItem, IDependentItemRef
    {
        string _type;
        SqlObjectProtoItem _protoItem;

        string _dataBase;
        string _schema;
        string _name;
        Version _version;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        NamedDependentItemContainerRef _container;

        internal SqlObjectItem( SqlObjectProtoItem p )
        {
            _protoItem = p;
            _type = p.ItemType;
            Database = p.DatabaseName;
            Schema = p.Schema;
            _name = p.Name;
            _version = p.Version;
            if( p.Requires != null ) Requires.Add( p.Requires );
            if( p.RequiredBy != null ) RequiredBy.Add( p.RequiredBy );
            if( p.Groups != null ) Groups.Add( p.Groups );
            if( p.Container != null ) _container = new NamedDependentItemContainerRef( p.Container );
        }

        /// <summary>
        /// Gets the <see cref="Schema"/>.<see cref="Name"/> name of this object.
        /// </summary>
        public string SchemaName
        {
            get { return _schema + '.' + _name; }
        }

        /// <summary>
        /// Gets or sets the schema name.
        /// Defaults to <see cref="SqlDatabase.DefaultSchemaName"/> ("CK").
        /// </summary>
        public string Schema
        {
            get { return _schema; }
            set { _schema = String.IsNullOrWhiteSpace( value ) ? SqlDatabase.DefaultSchemaName : value; }
        }

        /// <summary>
        /// Gets or sets the database logical name.
        /// Defaults to <see cref="SqlDatabase.DefaultDatabaseName"/> ("db").
        /// </summary>
        /// <remarks>
        /// This Database property is the logical name of a database, by no way should it be used as the actual, physical name of a database in any script.
        /// </remarks>
        public string Database
        {
            get { return _dataBase ?? SqlDatabase.DefaultDatabaseName; }
            set { _dataBase = String.IsNullOrWhiteSpace( value ) ? null : value; }
            //get { return _dataBase; }
            //set { _dataBase = String.IsNullOrWhiteSpace( value ) ? SqlDatabase.DefaultDatabaseName : value; }
        }

        /// <summary>
        /// Gets or sets the object name without <see cref="Database"/> nor <see cref="Schema"/>.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public IDependentItemList Requires
        {
            get { return _requires ?? (_requires = new DependentItemList()); }
        }

        public IDependentItemList RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new DependentItemList()); }
        }

        public IDependentItemGroupList Groups
        {
            get { return _groups ?? (_groups = new DependentItemGroupList()); }
        }

        /// <summary>
        /// Gets or sets the version number. Can be null.
        /// </summary>
        /// <remarks>
        /// When code builds the object, it may be safer to let the version be null and to rewrite the object.
        /// </remarks>
        public Version Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets the [<see cref="Database"/>]<see cref="SchemaName"/> full name of this object: the database name is the context of the object.
        /// Note that no prefix is added when Database is <see cref="SqlDatabase.DefaultDatabaseName"/>.
        /// </summary>
        public string FullName
        {
            get { return ContextNaming.FormatContextPrefix( SchemaName, _dataBase ); }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _container != null ? _container.EnsureContextPrefix( _dataBase ) : null; }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return null; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires.EnsureContextPrefix( _dataBase ); }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy.EnsureContextPrefix( _dataBase ); }
        }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get { return _groups.EnsureContextPrefix( _dataBase ); }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _protoItem != null ? _protoItem.PreviousNames : null; }
        }

        string IVersionedItem.ItemType
        {
            get { return _type; }
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

        object IDependentItem.StartDependencySort()
        { 
            return typeof(SqlObjectSetupDriver);
        }

        /// <summary>
        /// Writes the drop instruction.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteDrop( TextWriter b )
        {
            b.Write( "if OBJECT_ID('" );
            b.Write( SchemaName );
            b.Write( "') is not null drop " );
            b.Write( _type );
            b.Write( ' ' );
            b.Write( SchemaName );
            b.WriteLine( ';' );
        }

        /// <summary>
        /// Writes the whole object.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        public void WriteCreate( TextWriter b )
        {
            if( _protoItem != null ) b.WriteLine( _protoItem.Header );
            b.Write( "create " );
            b.Write( _type );
            b.Write( ' ' );
            b.Write( SchemaName );
            if( _protoItem != null ) b.WriteLine( _protoItem.TextAfterName );
        }


    }
}
