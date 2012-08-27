using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlDatabase
    {
        public static readonly string DefaultDatabaseName = "db";

        string _name;
        Dictionary<string,string> _schemas;

        public SqlDatabase()
        {
            _schemas = new Dictionary<string, string>( StringComparer.InvariantCultureIgnoreCase );
        }

        /// <summary>
        /// Gets or sets the logical name of the database.
        /// </summary>
        public string Name 
        {
            get { return _name; }
            set
            {
                if( String.IsNullOrWhiteSpace( value ) ) throw new ArgumentNullException( "value" );
                if( IsDefaultDatabase && value != DefaultDatabaseName ) throw new CKException( "Can not modify DefaultDatabaseName (it must be '{0}').", DefaultDatabaseName );
                _name = value;
            }
        }

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

        public virtual bool IsDefaultDatabase
        {
            get { return false; }
        }

    }
}
