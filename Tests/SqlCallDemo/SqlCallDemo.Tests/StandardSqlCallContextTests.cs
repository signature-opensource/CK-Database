using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using NUnit.Framework;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class StandardSqlCallContextTests
    {

        [Test]
        public void when_builder_function_throws()
        {
            AsynCallCatch<IndexOutOfRangeException>( "select 1;", command => (int)command.Parameters[0].Value );
            AsynCallCatch<InvalidCastException>( "select 1;", command => (int)(object)DBNull.Value );
        }

        [Test]
        public void when_SqlCommand_throws_a_SqlException()
        {
            AsynCallCatch<SqlException>( "select * frome kexistepas;", command => 3 );
        }

        [Test]
        public void when_acquire_connection_throws_ArgumentException_when_connection_string_is_syntax_invalid()
        {
            AsynCallCatch<ArgumentException>( "select 1;", command => 3, "%not a connection string at all%" );
        }

        [Test]
        public void when_acquire_connection_throws_SqlException_when_database_does_not_exist()
        {
            AsynCallCatch<SqlException>( "select 1;", command => 3, "Server=.;Database=kexistepas-db;Integrated Security=SSPI" );
        }

        [Test]
        [Explicit( "When trying to resolve a bad server name it takes a loooooooong time." )]
        public void when_acquire_connection_throws_ArgumentException_when_server_does_not_exist()
        {
            AsynCallCatch<SqlException>( "select 1;", command => 3, "Server=serverOfNothing;Database=ThisIsNotADatabase;Integrated Security=SSPI" );
        }

        void AsynCallCatch<TException>( string cmd, Func<SqlCommand,int> resultBuilder, string connectionString = null )
        {
            using( IDisposableSqlCallContext c = new SqlStandardCallContext() )
            using( var command = new SqlCommand( cmd ) )
            {
                try
                {
                    // If the asynchronous process is lost (if the exception is not correctly managed),
                    // this test will fail with a task Cancelled exception after:
                    // - 20 second when testing for connection string.... because when trying to resolve a bad server name it takes a loooooooong time.
                    // - 1 second in other cases.
                    CancellationTokenSource source = new CancellationTokenSource();
                    source.CancelAfter( connectionString == null ? 1000 : 20*1000 );
                    c.Executor
                        .ExecuteNonQueryAsyncTypedCancellable( connectionString ?? TestHelper.DatabaseTestConnectionString, command, resultBuilder, source.Token )
                        .Wait();
                }
                catch( AggregateException ex )
                {
                    Assert.That( ex.GetBaseException(), Is.InstanceOf<TException>() );
                }
            }
        }

    }
}
