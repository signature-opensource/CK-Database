using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Extends <see cref="ContextLocName"/> to split <see cref="ContextLocName.Name"/> into <see cref="Schema"/> and <see cref="ObjectName"/>.
    /// </summary>
    public class SqlContextLocName : ContextLocName 
    {
        string _schema;
        string _objectName;

        /// <summary>
        /// Initializes a new empty <see cref="SqlContextLocName"/>.
        /// </summary>
        public SqlContextLocName()
        {
            _objectName = string.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="SqlContextLocName"/> with a full name.
        /// If the name does not contain a schema <see cref="Schema"/> is null (unknown).
        /// </summary>
        /// <param name="fullName">Initial full name.</param>
        public SqlContextLocName( string fullName )
            : base( fullName )
        {
            _schema = DefaultContextLocNaming.SplitNamespace( Name, out _objectName );
            if( _schema.Length == 0 ) _schema = null;
        }

        /// <summary>
        /// Initializes a new <see cref="SqlContextLocName"/> with context, location and name with its schema.
        /// If the name does not contain a schema <see cref="Schema"/> is null (unknown).
        /// </summary>
        public SqlContextLocName( string context, string location, string schemaName )
            : base( context, location, schemaName )
        {
            _schema = DefaultContextLocNaming.SplitNamespace( Name, out _objectName );
            if( _schema.Length == 0 ) _schema = null;
        }

        /// <summary>
        /// Initializes a new <see cref="SqlContextLocName"/> with context, location schema and object name.
        /// Schema can be null (unknown) or empty (no schema).
        /// </summary>
        public SqlContextLocName( string context, string location, string schema, string objectName )
            : base( context, location, String.Empty )
        {
            _schema = schema;
            ObjectName = objectName;
        }

        /// <summary>
        /// Initializes a new <see cref="SqlContextLocName"/> from a context, a schema and object name.
        /// Schema can be null (unknown) or empty (no schema).
        /// </summary>
        public SqlContextLocName( IContextLocNaming context, string schema, string objectName )
            : this( context.Context, context.Location, schema, objectName )
        {
        }

        /// <summary>
        /// Gets or sets the name without schema. 
        /// This is never null (like <see cref="ContextLocName.Name"/>).
        /// </summary>
        public string ObjectName
        {
            get { return _objectName; }
            set 
            {
                if( value == null ) value = string.Empty;
                if( _objectName != value )
                {
                    _objectName = value;
                    Name = string.IsNullOrEmpty( _schema ) ? value : _schema + '.' + value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the schema. This can be null (unknown) or empty (there is explicitely no schema).
        /// </summary>
        public string Schema
        {
            get { return _schema; }
            set 
            { 
                if( _schema != value )
                {
                    bool oldNoSchema = String.IsNullOrEmpty( _schema );
                    _schema = value;
                    bool noSchema = String.IsNullOrEmpty( _schema );
                    if( oldNoSchema && noSchema ) return;
                    Name = noSchema ? _objectName : _schema + '.' + _objectName;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="Schema"/> is known.
        /// </summary>
        public bool IsSchemaKnown
        {
            get { return _schema != null; }
            set
            {
                if( !value ) _schema = null;
                else if( _schema == null ) _schema = string.Empty;
            }
        }

        protected override void OnNameChanged()
        {
            string newSchema = DefaultContextLocNaming.SplitNamespace( Name, out _objectName );
            if( newSchema.Length == 0 && string.IsNullOrEmpty( _schema ) ) return;
            _schema = newSchema;
        }

    }
}
