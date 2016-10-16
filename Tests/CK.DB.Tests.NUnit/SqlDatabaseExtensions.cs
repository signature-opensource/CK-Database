using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer.Setup;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CK.Core
{
    public static class SqlDatabaseExtensions
    {

        /// <summary>
        /// Checks that all invariants registered in CKCore.tInvariant are successful.
        /// </summary>
        /// <param name="this">This database.</param>
        public static SqlDatabase AssertAllCKCoreInvariant( this SqlDatabase @this )
        {
            @this.AssertEmptyReader( "exec CKCore.sInvariantRunAll; select InvariantKey, CountSelect, RunStatus from CKCore.tInvariant where RunStatus <> 'Success'" );
            return @this;
        }

        /// <summary>
        /// Checks that the <paramref name="selectClause"/> with its optional parameters @0, @1... returns no results.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        public static SqlDatabase AssertEmptyReader( this SqlDatabase @this, string selectClause, params object[] parameters )
        {
            AssertEmptyReader( @this.ConnectionString, selectClause, parameters );
            return @this;
        }

        /// <summary>
        /// Checks that the <paramref name="selectClause"/> with its optional parameters @0, @1... returns an expected result.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="expectedValue">Expected value of the <paramref name="selectClause"/>.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        public static SqlDatabase AssertScalarEquals( this SqlDatabase @this, object expectedValue, string selectClause, params object[] parameters )
        {
            AssertScalar( @this.ConnectionString, Is.EqualTo( expectedValue ), selectClause, parameters );
            return @this;
        }

        /// <summary>
        /// Checks that the <paramref name="selectClause"/> with its optional parameters @0, @1... returns a 
        /// scalar value that satisfies a constraint.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="constraint">The NUnit constraint to satisfy.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        public static SqlDatabase AssertScalar( this SqlDatabase @this, Constraint constraint, string selectClause, params object[] parameters )
        {
            AssertScalar( @this.ConnectionString, constraint, selectClause, parameters );
            return @this;
        }

        /// <summary>
        /// Executes a raw command and returns the number of rows affected.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="clause">String to execute.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        /// <returns>Numbers of rows affected.</returns>
        static public SqlDatabase AssertRawExecute( this SqlDatabase @this, int rowsAffected, string clause, params object[] parameters )
        {
            Assert.That( RawExecute( @this.ConnectionString, clause, parameters ), Is.EqualTo( rowsAffected ) );
            return @this;
        }

        /// <summary>
        /// Returns the object text definition of <paramref name="schemaName"/>.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="schemaname">Namme of the object.</param>
        /// <returns>The text.</returns>
        public static string GetObjectDefinition( this SqlDatabase @this, string schemaName )
        {
            return GetObjectDefinition( @this.ConnectionString, schemaName );
        }

        /// <summary>
        /// Executes the <paramref name="command"/>.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="command">The command to execute.</param>
        public static void ExecuteNonQuery( this SqlDatabase @this, string command )
        {
            using( var cmd = new SqlCommand( command ) ) @this.ExecuteNonQuery( cmd );
        }

        /// <summary>
        /// Executes the <paramref name="command"/>.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="command">The command to execute.</param>
        public static void ExecuteNonQuery( this SqlDatabase @this, SqlCommand command )
        {
            using( var c = new SqlConnection( @this.ConnectionString ) )
            {
                c.Open();
                command.Connection = c;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Checks that the <paramref name="selectClause"/> with its optional parameters @0, @1... returns no results.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        public static void AssertEmptyReader( string connectionString, string selectClause, params object[] parameters )
        {
            Execute( connectionString, selectClause, parameters, cmd =>
            {
                using( var reader = cmd.ExecuteReader( System.Data.CommandBehavior.SingleRow ) )
                {
                    var d = new SimpleDataTable( reader );
                    if( d.Rows.Count > 0 )
                    {
                        throw new AssertionException( d.PrettyPrint() );
                    }
                }
            } );
        }

        /// <summary>
        /// Checks that the <paramref name="selectClause"/> with its optional parameters @0, @1... returns a given scalar.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="expectedValue">Expected value of the <paramref name="selectClause"/>.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        public static void AssertScalarEquals( string connectionString, object expectedValue, string selectClause, params object[] parameters )
        {
            AssertScalar( connectionString, Is.EqualTo( expectedValue ), selectClause, parameters );
        }

        /// <summary>
        /// Checks that the <paramref name="selectClause"/> with its optional parameters @0, @1... returns a scalar
        /// that satifies the NUnit constraint.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="constraint">Constraint tha must match the returned value.</param>
        /// <param name="selectClause">The select clause.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        public static void AssertScalar( string connectionString, Constraint constraint, string selectClause, params object[] parameters )
        {
            Execute( connectionString, selectClause, parameters, cmd =>
            {
                Assert.That( cmd.ExecuteScalar(), constraint );
            } );
        }

        /// <summary>
        /// Returns the object text definition of <paramref name="schemaName"/> object.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="schemaname">Namme of the object.</param>
        /// <returns>The text.</returns>
        public static string GetObjectDefinition( string connectionString, string schemaName )
        {
            string r = null;
            Execute( connectionString, "select OBJECT_DEFINITION(OBJECT_ID(@0))", new string[] { schemaName }, cmd =>
                {
                    r = (string)cmd.ExecuteScalar();
                } );
            return r;
        }

        /// <summary>
        /// Executes a raw command and returns the number of rows affected.
        /// </summary>
        /// <param name="this">This database.</param>
        /// <param name="clause">String to execute.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        /// <returns>Numbers of rows affected.</returns>
        static public int RawExecute( this SqlDatabase db, string clause, params object[] parameters )
        {
            return RawExecute( db.ConnectionString, clause, parameters );
        }
        
        /// <summary>
        /// Executes a raw command and returns the number of rows affected.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="clause">String to execute.</param>
        /// <param name="parameters">Parameters that will replace @0, @1,...@n placeholders in <paramref name="selectClause"/>.</param>
        /// <returns>Numbers of rows affected.</returns>
        static public int RawExecute( string connectionString, string clause, params object[] parameters )
        {
            int rows = 0;
            Execute( connectionString, clause, parameters, cmd =>
            {
                rows = cmd.ExecuteNonQuery();
            } );
            return rows;
        }

        static void Execute( string connectionString, string selectClause, object[] parameters, Action<SqlCommand> action )
        {
            using( var oCon = new SqlConnection( connectionString ) )
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
