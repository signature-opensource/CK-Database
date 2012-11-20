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
        ContextLocNameStructImpl _fullName;
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
            _fullName = new ContextLocNameStructImpl();
            _protoItem = p;
            _type = p.ItemType;
            //??
            Database = p.PhysicalDatabaseName;
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
            get { return _fullName.Name; }
        }

        /// <summary>
        /// Gets or sets the schema name.
        /// Defaults to <see cref="SqlDatabase.DefaultSchemaName"/> ("CK").
        /// </summary>
        public string Schema
        {
            get { return _schema; }
            set 
            {
                if( String.IsNullOrWhiteSpace( value ) ) value = SqlDatabase.DefaultSchemaName;
                if( _schema != value )
                {
                    _schema = value;
                    _fullName.Name = _schema + '.' + _name;
                }
            }
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
            get { return _fullName.Location; }
            set 
            { 
                _fullName.Location = String.IsNullOrWhiteSpace( value ) ? SqlDatabase.DefaultDatabaseName : value;
                _fullName.Context = _fullName.Location != SqlDatabase.DefaultDatabaseName ? _fullName.Location : String.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the object name without <see cref="Database"/> nor <see cref="Schema"/>.
        /// Defaults.to <see cref="String.Empty"/>.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set 
            {
                if( value == null ) value = String.Empty;
                if( _name != value )
                {
                    _name = value;
                    _fullName.Name = _schema + '.' + _name;
                }

            }
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
        /// Gets the full name of this object.
        /// </summary>
        public string FullName
        {
            get { return _fullName.FullName; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _container.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _fullName.Context, _fullName.Location ) ); }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return null; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _fullName.Context, _fullName.Location ) ); }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _fullName.Context, _fullName.Location ) ); }
        }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get { return _groups.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _fullName.Context, _fullName.Location ) ); }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _protoItem != null ? _protoItem.PreviousNames.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, _fullName.Context, _fullName.Location ) ) : null; }
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
