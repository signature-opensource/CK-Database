using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{

    [Setup( ItemType = typeof( SqlDatabaseItem ), ItemKind = DependentItemType.Group, TrackAmbientProperties = TrackAmbientPropertiesMode.AddPropertyHolderAsChildren )]
    public class SqlDatabase
    {
        /// <summary>
        /// Default database name is <see cref="String.Empty"/>. 
        /// This is required so that the default database maps to the default context of <see cref="IAmbientContract"/> objects.
        /// </summary>
        public const string DefaultDatabaseName = "";

        /// <summary>
        /// Default schema name is "CK".
        /// </summary>
        public const string DefaultSchemaName = "CK";

        string _name;
        Dictionary<string,string> _schemas;
        bool _installCore;

        public SqlDatabase()
        {
            Debug.Assert( DefaultDatabaseName.Length == 0 );
            _name = DefaultDatabaseName;
            _schemas = new Dictionary<string, string>( StringComparer.InvariantCultureIgnoreCase );
        }

        /// <summary>
        /// Gets or sets the logical name of the database.
        /// Defaults to <see cref="DefaultDatabaseName"/>.
        /// </summary>
        public string Name
        {
            get { return _name; }
            protected set
            {
                if( String.IsNullOrWhiteSpace( value ) ) throw new ArgumentNullException( "value" );
                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public string ConnectionString { get; protected set; }

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
        /// Default database name is <see cref="DefaultDatabaseName"/> = <see cref="String.Empty"/>.
        /// </summary>
        public bool IsDefaultDatabase
        {
            get { return _name.Length == 0; }
        }
    }
}
