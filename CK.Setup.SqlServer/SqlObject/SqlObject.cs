using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    public class SqlObject : IVersionedItem, IDependentItemRef
    {
        static public readonly string TypeView = "View";
        static public readonly string TypeProcedure = "Procedure";
        static public readonly string TypeFunction = "Function";

        public class ReadInfo
        {
            public string DatabaseName { get; private set; }
            public string Schema { get; private set; }
            public string Name { get; private set; }
            
            public string Header { get; private set; }
            public Version Version { get; private set; }
            public string PackageName { get; private set; }
            public IEnumerable<string> Requires { get; private set; }
            public IEnumerable<string> RequiredBy { get; private set; }
            public IEnumerable<VersionedName> PreviousNames { get; private set; }
            public string TextAfterName { get; private set; }
            
            internal ReadInfo(
                        string databaseName,
                        string schema,
                        string name,
                        string header,
                        Version v,
                        string packageName,
                        IEnumerable<string> requires, 
                        IEnumerable<string> requiredBy, 
                        IEnumerable<VersionedName> prevNames,
                        string textAfterName )
            {
                DatabaseName = databaseName;
                Schema = schema;
                Name = name;
                Header = header;
                Version = v;
                PackageName = packageName;
                Requires = requires;
                RequiredBy = requiredBy;
                PreviousNames = prevNames;
                TextAfterName = textAfterName;
            }
        }

        string _type;
        ReadInfo _readInfo;

        string _schema;
        string _name;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        IDependentItemContainerRef _container;

        internal SqlObject( string type, ReadInfo readInfo )
        {
            _type = type;
            _requires = new DependentItemList();
            _requiredBy = new DependentItemList();

            _readInfo = readInfo;
            _schema = readInfo.Schema;
            _name = readInfo.Name;
            _requires.Add( readInfo.Requires );
            _requiredBy.Add( readInfo.RequiredBy );
            if( readInfo.PackageName != null ) _container = new NamedDependentItemContainerRef( readInfo.PackageName );

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
            get { return _requires; }
        }

        public IDependentItemList RequiredBy
        {
            get { return _requiredBy; }
        }

        // Version exists only for text based object.
        // When code builds the object, it is safer to let the version be null
        // and to rewrite the object.
        Version IVersionedItem.Version
        {
            get { return _readInfo != null ? _readInfo.Version : null; }
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

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _readInfo != null ? _readInfo.PreviousNames : null; }
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
            return typeof(SqlObjectDriver).AssemblyQualifiedName;
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
            if( _readInfo != null ) b.WriteLine( _readInfo.Header );
            b.Write( "create " );
            b.Write( _type );
            b.Write( ' ' );
            b.Write( SchemaName );
            if( _readInfo != null ) b.WriteLine( _readInfo.TextAfterName );
        }


    }
}
