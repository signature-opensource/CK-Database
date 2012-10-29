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

        string _schema;
        string _name;
        Version _version;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        DependentItemGroupList _groups;
        IDependentItemContainerRef _container;

        internal SqlObjectItem( SqlObjectProtoItem p )
        {
            _type = type;
            _protoItem = readInfo;
            _schema = readInfo.Schema;
            _name = readInfo.Name;
            if( readInfo.Requires != null ) Requires.Add( readInfo.Requires );
            if( readInfo.RequiredBy != null ) RequiredBy.Add( readInfo.RequiredBy );
            if( readInfo.Groups != null ) Groups.Add( readInfo.Groups );
            if( readInfo.Container != null ) _container = new NamedDependentItemContainerRef( readInfo.Container );
        }

        /// <summary>
        /// Replaces this information with the one from a <see cref="SqlObjectProtoItem"/>.
        /// </summary>
        /// <param name="logger">Used to log whenever a replacement occurs.</param>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool ReplaceWith( IActivityLogger logger, SqlObjectProtoItem p )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            bool nameChanged = false;
            if( p != null )
            {
                if( p.Name != null )
                {
                    if( _name != null && _name != p.Name )
                    {
                        logger.Warn( "Item '{0}' changed its name from '{1}' to '{2}'.", ToString(), _name, p.Name );
                        nameChanged = true;
                    }
                    _name = p.Name;
                }
                if( p.ItemType != null )
                {
                    if( _type != null && _type != p.ItemType )
                    {
                        logger.Error( "Item '{0}' changed its type from '{1}' to '{2}'.", ToString(), _type, p.ItemType );
                    }
                    _type = p.ItemType;
                }
                if( p.Schema != null )
                {
                    if( _schema != null && _schema != p.Schema )
                    {
                        logger.Warn( "Item '{0}' changed its Schema from '{1}' to '{2}'.", ToString(), _schema, p.Schema );
                        nameChanged = true;
                    }
                    _schema = p.Schema;
                }
                if( p.Container != null )
                {
                    if( _container != null && _container.FullName != p.Container )
                    {
                        logger.Warn( "Item '{0}' changed its Container from '{1}' to '{2}'.", ToString(), _container.FullName, p.Container );
                    }
                    _container = new NamedDependentItemContainerRef( p.Container );
                }
                if( p.Version != null )
                {
                    if( _version != null && _version != p.Version )
                    {
                        logger.Warn( "Item '{0}' changed its Version from '{1}' to '{2}'.", ToString(), _version, p.Version );
                    }
                    _container = new NamedDependentItemContainerRef( p.Container );
                }
                if( p.Requires != null ) 
                {
                    Requires.Clear(); 
                    Requires.Add( p.Requires ); 
                }
                if( p.RequiredBy != null ) 
                { 
                    RequiredBy.Clear(); 
                    RequiredBy.Add( p.RequiredBy ); 
                }
                if( p.Groups != null )
                {
                    Groups.Clear();
                    Groups.Add( p.Groups );
                }
            }
            return !nameChanged;
        }

        public string SchemaName
        {
            get { return _schema + '.' + _name; }
        }

        public string Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }

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

        public string FullName
        {
            get { return SchemaName; }
        }

        IDependentItemContainerRef IDependentItem.Container
        {
            get { return _container; }
        }

        IDependentItemRef IDependentItem.Generalization
        {
            get { return null; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.Requires
        {
            get { return _requires; }
        }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy
        {
            get { return _requiredBy; }
        }

        IEnumerable<IDependentItemGroupRef> IDependentItem.Groups
        {
            get { return _groups; }
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
            return typeof(SqlObjectSetupDriver).AssemblyQualifiedName;
        }

        /// <summary>
        /// Writes the drop instruction.
        /// </summary>
        /// <param name="b">The _specialization <see cref="TextWriter"/>.</param>
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
        /// <param name="b">The _specialization <see cref="TextWriter"/>.</param>
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
