using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace CK.Testing
{
    public static class CKSqlServerTestHelperExtensions
    {
        /// <summary>
        /// Clears schemas from a database, using <see cref="ICKSqlServerTestHelperCore.UsedSchemas"/>
        /// and <see cref="ISqlServerTestHelperCore.DatabaseTestName"/> by default.
        /// </summary>
        /// <param name="this">This helper.</param>
        /// <param name="connectionString">Connection to use. Defaults to the test database.</param>
        /// <param name="usedSchemas">Set of schemas to destroy.</param>
        public static void ClearDatabaseUsedSchemas( this ICKSqlServerTestHelper @this, IEnumerable<string> usedSchemas = null, string connectionString = null )
        {
            connectionString = connectionString ?? @this.DatabaseTestConnectionString;
            usedSchemas = usedSchemas ?? @this.UsedSchemas;
            var monitor = @this.Monitor;
            using( monitor.OpenInfo( $"Clearing schemas {usedSchemas.Concatenate()} ({connectionString})." ) )
            using( var oCon = new SqlConnection( connectionString ) )
            {
                oCon.Open();
                int maxTryCount = usedSchemas.Count();
                bool retry;
                do
                {
                    retry = false;
                    foreach( var s in usedSchemas )
                    {
                        if( s == "CKCore" )
                        {
                            monitor.Trace( "Removing 'CKCore' objets." );
                            retry |= !SchemaDropAllObjects( monitor, oCon, "CKCore", false );
                        }
                        else
                        {
                            monitor.Trace( $"Removing '{s}' schema and its objects." );
                            retry |= !SchemaDropAllObjects( monitor, oCon, s, true );
                        }
                    }
                }
                while( --maxTryCount >= 0 && retry );
                if( retry ) throw new CKException( "Unable to clear all schemas." );
            }
        }

        static bool SchemaDropAllObjects( IActivityMonitor monitor, SqlConnection oCon, string schema, bool dropSchema )
        {
            if( string.IsNullOrEmpty( schema )
                || schema.IndexOf( '\'' ) >= 0
                || schema.IndexOf( ';' ) >= 0 ) throw new ArgumentException( nameof( schema ) );
            try
            {
                using( var c = new SqlCommand( "CKCore.sSchemaDropAllObjects" ) { Connection = oCon } )
                {
                    c.CommandType = CommandType.StoredProcedure;
                    c.Parameters.AddWithValue( "@SchemaName", schema );
                    c.ExecuteNonQuery();
                    if( dropSchema )
                    {
                        c.CommandType = CommandType.Text;
                        c.CommandText = $"if exists(select 1 from sys.schemas where name = '{schema}') drop schema [{schema.Replace( "]", "]]" )}];";
                        c.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch( Exception ex )
            {
                monitor.Error( ex );
                return false;
            }
        }

    }
}
