using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{

    [Setup( ItemKind = DependentItemKindSpec.Group, TrackAmbientProperties = TrackAmbientPropertiesMode.AddPropertyHolderAsChildren, ItemTypeName = "CK.SqlServer.Setup.SqlDatabaseItem,CK.SqlServer.Setup.Runtime" )]
    public class SqlDatabase
    {
        /// <summary>
        /// Default database name is "db": this is the name of the <see cref="SqlDefaultDatabase"/> type.
        /// </summary>
        public const string DefaultDatabaseName = "db";

        /// <summary>
        /// Default schema name is "CK".
        /// </summary>
        public const string DefaultSchemaName = "CK";

        readonly string _name;
        readonly Dictionary<string,string> _schemas;
        bool _installCore;

        public SqlDatabase( string name )
        {
            if( String.IsNullOrWhiteSpace( name ) ) throw new ArgumentException( "Must be not null, empty, nor whitespace.", "name" );
            _name = name;
            _schemas = new Dictionary<string, string>( StringComparer.InvariantCultureIgnoreCase );
        }

        /// <summary>
        /// Gets the logical name of the database. 
        /// This name, which is strongly associated to this SqlDatabase object and can not be changed (set only in the constructor), 
        /// defines the location of objects that are bound to it and drives the actual connection string to use.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets (or sets for inherited classes) the connection string.
        /// This can be automatically configured during setup (if the specialized class implements a Construct method with a connectionString parameter
        /// and sets this property).
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Finds or creates the given schema. 
        /// Schema names are case sensitive and this constraint is enforced here: an exception will 
        /// be thrown whenever casing differ between schema registration.
        /// </summary>
        /// <param name="name">Name of the schema.</param>
        /// <returns>Registered name.</returns>
        public string EnsureSchema( string name )
        {
            if( String.IsNullOrWhiteSpace( name ) ) throw new ArgumentException( "Must be not null, empty, nor whitespace.", "name" );
            string existing;
            if( _schemas.TryGetValue( name, out existing ) )
            {
                if( name != existing )
                {
                    throw new CKException( "Casing must be strictly the same. '{0}' differs from '{1}'.", name, existing ); 
                }
            }
            else _schemas.Add( name, (existing = name) );
            return existing;
        }

        /// <summary>
        /// Gets the different schemas that are owned by this <see cref="SqlDatabase"/>.
        /// </summary>
        public IEnumerable<string> Schemas
        {
            get { return _schemas.Keys; }
        }

        /// <summary>
        /// Gets or sets whether CK Core kernel support must be installed in the database.
        /// Defaults to false.
        /// Always true if <see cref="IsDefaultDatabase"/> is true.
        /// </summary>
        public bool InstallCore 
        {
            get { return _installCore | IsDefaultDatabase; }
            set { _installCore = value; } 
        }

        /// <summary>
        /// Default database name is <see cref="DefaultDatabaseName"/> = "db".
        /// </summary>
        public bool IsDefaultDatabase
        {
            get { return _name == DefaultDatabaseName; }
        }
    }
}
