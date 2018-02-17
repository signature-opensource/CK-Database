using CK.SqlServer;
using NUnit.Framework;
using System;
using System.Data.SqlClient;
using System.Threading;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class StandardSqlCallContextTests
    {
        [Test]
        public void exec_SqlCommand_throws_a_SqlDetailedException_when_a_SqlException_is_thrown()
        {
            AsyncCallCatch<SqlDetailedException>( "select * frome kexistepas;" );
        }

        [Test]
        public void exec_throws_ArgumentException_when_connection_string_is_syntax_invalid()
        {
            AsyncCallCatch<ArgumentException>( "select 1;", "%not a connection string at all%" );
        }

        [Test]
        public void exec_throws_SqlDetailedException_when_database_does_not_exist()
        {
            AsyncCallCatch<SqlDetailedException>( "select 1;", TestHelper.GetConnectionString( "kexistepas-db" ) );
        }

        [Test]
        [Explicit( "When trying to resolve a bad server name it takes a loooooooong time." )]
        public void exec_throws_SqlDetailedException_when_server_does_not_exist()
        {
            AsyncCallCatch<SqlDetailedException>( "select 1;", "Server=serverOfNothing;Database=ThisIsNotADatabase;Integrated Security=SSPI" );
        }

        void AsyncCallCatch<TException>( string cmd, string connectionString = null )
        {
            using( IDisposableSqlCallContext c = new SqlStandardCallContext() )
            using( var command = new SqlCommand( cmd ) )
            {
                try
                {
                    // If the asynchronous process is lost (if the exception is not correctly managed),
                    // this test will fail with a task Cancelled exception after:
                    // - 30 second when testing for connection string.... because when trying to resolve a bad server name it takes a loooooooong time.
                    // - 1 second in other cases.
                    CancellationTokenSource source = new CancellationTokenSource();
                    source.CancelAfter( connectionString == null ? 1000 : 30*1000 );
                    c.Executor
                        .ExecuteNonQueryAsync( connectionString ?? TestHelper.GetConnectionString(), command, source.Token )
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
