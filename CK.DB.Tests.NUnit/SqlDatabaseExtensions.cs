using System;
using Microsoft.Data.SqlClient;
using System.Text;
using CK.SqlServer;
using CK.Testing;

namespace CK.Core
{
    /// <summary>
    /// Provides extension methods to <see cref="SqlDatabase"/>.
    /// </summary>
    public static class SqlDatabaseExtensions
    {
        /// <summary>
        /// Gets the violations for all invariants or a subset of them. 
        /// The data table is empty if no violation exist.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="invariantName">Optional subset of invariant names to check.</param>
        /// <returns>The violations.</returns>
        public static SimpleDataTable GetCKCoreInvariantsViolations( this SqlDatabase @this, params string[] invariantName )
        {
            const string check = "select InvariantKey, CountSelect, RunStatus from CKCore.tInvariant where Ignored = 0 and RunStatus <> 'Success' and RunStatus <> 'Never ran'";
            if( invariantName.Length == 0 )
            {
                return ExecuteReader( @this, "exec CKCore.sInvariantRunAll;" + check )!;
            }
            StringBuilder b = new StringBuilder();
            foreach( var i in invariantName )
            {
                if( string.IsNullOrWhiteSpace( i ) ) throw new ArgumentException( "invariantName must not be null or white space." );
                b.Append( "exec CKCore.sInvariantRun '" )
                    .Append( SqlHelper.SqlEncodeStringContent( i ) )
                    .Append( "';" );
            }
            b.Append( check );
            return ExecuteReader( @this, b.ToString() )!;
        }

        /// <summary>
        /// Applies a temporary transformation. The transformer must target an existing
        /// sql object that will be restored when the returned IDisposable.Dispose() method is called. 
        /// </summary>
        /// <param name="this">This SqlDatabase.</param>
        /// <param name="transformer">Transformer text.</param>
        /// <returns>A disposable object that will restore the original object.</returns>
        public static IDisposable TemporaryTransform( this SqlDatabase @this, string transformer )
        {
            return SqlTransformTestHelper.TestHelper.TemporaryTransform( @this.ConnectionString, transformer );
        }

        /// <summary>
        /// Reads the first row.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        /// <returns>First row values or null if there is none.</returns>
        public static object[]? ReadFirstRow( this SqlDatabase @this, string selectClause, params object[] parameters )
        {
            object[]? result = null;
            Execute( @this, selectClause, parameters, cmd =>
            {
                using( var reader = cmd.ExecuteReader() )
                {
                    if( reader.Read() )
                    {
                        result = new object[reader.FieldCount];
                        reader.GetValues( result );
                    }
                }
            } );
            return result;
        }

        /// <summary>
        /// Executes the <paramref name="selectClause"/> and returns the scalar result or null if the result set is empty.
        /// Don't use this for big texts: a maximum of 2033 characters can be returned.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        /// <returns>The scalar or null if the result set is empty.</returns>
        public static object? ExecuteScalar( this SqlDatabase @this, string selectClause, params object[] parameters )
        {
            object? result = null;
            Execute( @this, selectClause, parameters, cmd =>
            {
                result = cmd.ExecuteScalar();
            } );
            return result;
        }

        /// <summary>
        /// Executes the <paramref name="selectClause"/> and returns the scalar result or null if the result set is empty.
        /// Don't use this for big texts: a maximum of 2033 characters can be returned.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        /// <returns>The typed scalar.</returns>
        public static T? ExecuteScalar<T>( this SqlDatabase @this, string selectClause, params object[] parameters ) => (T?)@this.ExecuteScalar( selectClause, parameters );

        /// <summary>
        /// Reads the <paramref name="selectClause"/> with its optional parameters @0, @1...
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        /// <returns>A simple data table of the results.</returns>
        public static SimpleDataTable? ExecuteReader( this SqlDatabase @this, string selectClause, params object[] parameters )
        {
            SimpleDataTable? result = null;
            Execute( @this, selectClause, parameters, cmd =>
            {
                using( var reader = cmd.ExecuteReader() )
                {
                    result = new SimpleDataTable( reader );
                }
            } );
            return result;
        }

        /// <summary>
        /// Returns the object text definition of <paramref name="schemaName"/> object.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="schemaName">Name of the object.</param>
        /// <returns>The text.</returns>
        public static string GetObjectDefinition( this SqlDatabase @this, string schemaName )
        {
            return SqlTransformTestHelper.TestHelper.GetObjectDefinition( @this.ConnectionString, schemaName );
        }

        /// <summary>
        /// Executes a raw command and returns the number of rows affected.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>Numbers of rows affected.</returns>
        static public int ExecuteNonQuery( this SqlDatabase @this, SqlCommand cmd )
        {
            using( var c = new SqlConnection( @this.ConnectionString ) )
            {
                var saved = cmd.Connection;
                try
                {
                    c.Open();
                    cmd.Connection = c;
                    return cmd.ExecuteNonQuery();
                }
                finally
                {
                    cmd.Connection = saved;
                }
            }
        }

        /// <summary>
        /// Executes a scalar command.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>The result.</returns>
        static public object ExecuteScalar( this SqlDatabase @this, SqlCommand cmd )
        {
            using( var c = new SqlConnection( @this.ConnectionString ) )
            {
                var saved = cmd.Connection;
                try
                {
                    c.Open();
                    cmd.Connection = c;
                    return cmd.ExecuteScalar();
                }
                finally
                {
                    cmd.Connection = saved;
                }
            }
        }

        /// <summary>
        /// Executes a raw command and returns the number of rows affected.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="command">String to execute.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="command"/>.</param>
        /// <returns>Numbers of rows affected.</returns>
        static public int ExecuteNonQuery( this SqlDatabase @this, string command, params object[] parameters )
        {
            int rows = 0;
            Execute( @this, command, parameters, cmd =>
            {
                rows = cmd.ExecuteNonQuery();
            } );
            return rows;
        }

        static void Execute( SqlDatabase db, string selectClause, object[] parameters, Action<SqlCommand> action )
        {
            using( var oCon = new SqlConnection( db.ConnectionString ) )
            using( var cmd = new SqlCommand( selectClause, oCon ) )
            {
                if( parameters != null ) AddAutoParameters( cmd, parameters );
                oCon.Open();
                action( cmd );
            }
        }

        static void AddAutoParameters( SqlCommand cmd, object[] parameters )
        {
            for( int i = 0; i < parameters.Length; ++i )
            {
                cmd.Parameters.AddWithValue( String.Format( "@{0}", i ), parameters[i] );
            }
        }


    }
}
