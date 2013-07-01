using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlObjectItem : IVersionedItem, IDependentItemRef
    {
        internal readonly static Type TypeCommand = typeof( SqlCommand );
        internal readonly static Type TypeParameterCollection = typeof( SqlParameterCollection );
        internal readonly static Type TypeParameter = typeof( SqlParameter );
        internal readonly static ConstructorInfo SqlParameterCtor2 = TypeParameter.GetConstructor( new Type[] { typeof( string ), typeof( SqlDbType ) } );
        internal readonly static ConstructorInfo SqlParameterCtor3 = TypeParameter.GetConstructor( new Type[] { typeof( string ), typeof( SqlDbType ), typeof( Int32 ) } );

        internal readonly static MethodInfo MCommandSetCommandType = TypeCommand.GetProperty( "CommandType" ).GetSetMethod();
        internal readonly static MethodInfo MCommandGetParameters = TypeCommand.GetProperty( "Parameters", SqlObjectItem.TypeParameterCollection ).GetGetMethod();
        internal readonly static MethodInfo MParameterCollectionAddParameter = TypeParameterCollection.GetMethod( "Add", new Type[] { TypeParameter } );
        internal readonly static MethodInfo MParameterCollectionRemoveAtParameter = TypeParameterCollection.GetMethod( "RemoveAt", new Type[] { typeof( Int32 ) } );
        internal readonly static MethodInfo MParameterCollectionGetParameter = TypeParameterCollection.GetProperty( "Item", new Type[] { typeof( Int32 ) } ).GetGetMethod();

        internal readonly static MethodInfo MParameterSetDirection = TypeParameter.GetProperty( "Direction" ).GetSetMethod();
        internal readonly static MethodInfo MParameterSetValue = TypeParameter.GetProperty( "Value" ).GetSetMethod();
        internal readonly static MethodInfo MParameterGetValue = TypeParameter.GetProperty( "Value" ).GetGetMethod();
        internal readonly static FieldInfo FieldDBNullValue = typeof( DBNull ).GetField( "Value", BindingFlags.Public | BindingFlags.Static );


        ContextLocNameStructImpl _fullName;
        string _type;
        SqlObjectProtoItem _protoItem;

        string _physicalDB;
        string _schema;
        string _objectName;
        Version _version;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        IDependentItemContainerRef _container;
        bool? _missingDependencyIsError;

        internal SqlObjectItem( SqlObjectProtoItem p )
        {
            _fullName = new ContextLocNameStructImpl( p );
            _schema = p.Schema;
            _objectName = p.ObjectName;
            _protoItem = p;
            _type = p.ItemType;
            // Keeps the physical database name if the proto item defines it.
            // It is currently unused.
            _physicalDB = p.PhysicalDatabaseName;
            
            _version = p.Version;
            if( p.Requires != null ) Requires.Add( p.Requires );
            if( p.RequiredBy != null ) RequiredBy.Add( p.RequiredBy );
            if( p.Groups != null ) Groups.Add( p.Groups );
            if( p.Container != null ) _container = new NamedDependentItemContainerRef( p.Container );
            _missingDependencyIsError = p.MissingDependencyIsError;
        }

        /// <summary>
        /// Gets or sets the container of this object.
        /// </summary>
        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
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
                    _fullName.Name = _schema + '.' + _objectName;
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
        /// Defaults to <see cref="String.Empty"/>.
        /// </summary>
        public string ObjectName
        {
            get { return _objectName; }
            set 
            {
                if( value == null ) value = String.Empty;
                if( _objectName != value )
                {
                    _objectName = value;
                    _fullName.Name = _schema + '.' + _objectName;
                }

            }
        }

        /// <summary>
        /// Gets or sets whether when installing, the informational message 'The module 'X' depends 
        /// on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="LogLevel.Error"/>. When false, this is a <see cref="LogLevel.Info"/>.
        /// Sets first by MissingDependencyIsError is text, otherwise an attribute (that should default to true should be applied).
        /// When not set, it is considered to be true.
        /// </summary>
        public bool? MissingDependencyIsError
        {
            get { return _missingDependencyIsError; }
            set { _missingDependencyIsError = value; }
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
