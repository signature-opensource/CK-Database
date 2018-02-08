using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Text;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Extends <see cref="ContextLocName"/> to split <see cref="ContextLocName.Name"/> 
    /// into <see cref="Schema"/> and <see cref="ObjectName"/>.
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
            : base( context, location, string.Empty )
        {
            _schema = schema;
            ObjectName = objectName;
        }

        /// <summary>
        /// Copy constructor. Initializes a new <see cref="SqlContextLocName"/> from another SqlContextLocName.
        /// </summary>
        public SqlContextLocName( SqlContextLocName other )
            : base( other )
        {
            _schema = other._schema;
            _objectName = other._objectName;
        }

        /// <summary>
        /// Initializes a new <see cref="SqlContextLocName"/> from a context, a schema and object name.
        /// Schema can be null (unknown) or empty (no schema).
        /// </summary>
        public SqlContextLocName( IContextLocNaming context, string schema, string objectName )
            : this( context.Context, context.Location, schema, objectName )
        {
        }

        public override ContextLocName Clone() => new SqlContextLocName( this );

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

        static readonly string[] _allowedResourcePrefixes = new string[] { "[Replace]", "[Transform]" };

        /// <summary>
        /// Lookups a resource based on <see cref="GetResourceFileNameCandidates"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="packageItem">The package.</param>
        /// <param name="fileName">The file name found.</param>
        /// <returns>The resource text on success, null otherwise.</returns>
        public string LoadTextResource( IActivityMonitor monitor, SqlPackageBaseItem packageItem, out string fileName )
        {
            var candidates = GetResourceFileNameCandidates( packageItem );
            fileName = null;
            string text = null;
            foreach( var fName in candidates )
            {
                fileName = fName;
                if( (text = packageItem.ResourceLocation.GetString( fileName, false, _allowedResourcePrefixes )) != null ) break;
            }
            if( text == null )
            {
                monitor.Error( $"Resource '{FullName}' of '{packageItem.FullName}' not found. Tried: '{candidates.Concatenate( "' ,'" )}'." );
                return null;
            }
            if( fileName.EndsWith( ".y4" ) )
            {
                text = SqlPackageBaseItem.ProcessY4Template( monitor, null, packageItem, null, fileName, text );
            }
            return text;
        }

        /// <summary>
        /// Generates '.sql', '.y4' and '.tql' candidate files based on <see cref="GetResourceNameCandidates"/>.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <returns>Set of candidates.</returns>
        public IEnumerable<string> GetResourceFileNameCandidates( IContextLocNaming containerName )
        {
            var y4 = GetResourceNameCandidates( containerName ).Select( r => r + ".y4" );
            var sql = GetResourceNameCandidates( containerName ).Select( r => r + ".sql" );
            if( TransformArg != null )
            {
                var tql = GetResourceNameCandidates( containerName ).Select( r => r + ".tql" );
                return tql.Concat( sql ).Concat( y4 );
            }
            return sql.Concat( y4 );
        }

        /// <summary>
        /// Generates candidate names to look up resources or files.
        /// </summary>
        /// <param name="containerName">The container's name.</param>
        /// <returns>Set of candidates.</returns>
        public IEnumerable<string> GetResourceNameCandidates( IContextLocNaming containerName )
        {
            if( TransformArg != null )
            {
                if( FullName.StartsWith( containerName.FullName ) )
                {
                    SqlContextLocName t = new SqlContextLocName( TransformArg );
                    yield return t.ObjectName;
                    yield return t.Name;
                    SqlContextLocName simpler = new SqlContextLocName( null, null, null, ObjectName );
                    simpler.TransformArg = t.ObjectName; yield return simpler.FullName;
                    simpler.TransformArg = t.Name; yield return simpler.FullName;
                    simpler.TransformArg = t.FullName; yield return simpler.FullName;
                    if( !string.IsNullOrEmpty( Schema ) )
                    {
                        simpler.Schema = Schema;
                        simpler.TransformArg = t.ObjectName; yield return simpler.FullName;
                        simpler.TransformArg = t.Name; yield return simpler.FullName;
                        simpler.TransformArg = t.FullName; yield return simpler.FullName;
                    }
                    if( !string.IsNullOrEmpty( Location ) )
                    {
                        simpler.Location = Location;
                        simpler.TransformArg = t.ObjectName; yield return simpler.FullName;
                        simpler.TransformArg = t.Name; yield return simpler.FullName;
                        simpler.TransformArg = t.FullName; yield return simpler.FullName;
                    }
                    if( !string.IsNullOrEmpty( Context ) )
                    {
                        simpler.Context = Context;
                        simpler.TransformArg = t.ObjectName; yield return simpler.FullName;
                        simpler.TransformArg = t.Name; yield return simpler.FullName;
                        simpler.TransformArg = t.FullName; yield return simpler.FullName;
                    }
                    yield return t.FullName;
                }
            }
            yield return ObjectName;
            yield return Name;
            yield return FullName;
        }
    }
}
