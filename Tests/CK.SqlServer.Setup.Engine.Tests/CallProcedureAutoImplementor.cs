using System;
using NUnit.Framework;
using CK.Core;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using static CK.Testing.DBSetupTestHelper;
using FluentAssertions;

namespace CK.SqlServer.Setup.Engine.Tests
{

    public interface ISqlCallContext
    {
        IActivityMonitor Logger { get; }

        SqlConnection Connection { get; }
    }

    public class SqlCallContext : ISqlCallContext
    {
        readonly SqlManager _m;

        public SqlCallContext( SqlManager m )
        {
            _m = m;
        }

        public IActivityMonitor Logger
        {
            get { return TestHelper.Monitor; }
        }

        public SqlConnection Connection
        {
            get { return _m.Connection; }
        }

    }

    [TestFixture]
    public class CallProcedureAutoImplementor
    {
        readonly static string ConnectionString = TestHelper.GetConnectionString();

        class ManualCall
        {
            public SqlCommand StandardSP()
            {
                SqlCommand cmd = new SqlCommand( "CK.StupidTest" );
                cmd.CommandType = CommandType.StoredProcedure;
                SqlParameterCollection c = cmd.Parameters;
                SqlParameter p;
                p = new SqlParameter( "@x", SqlDbType.Int );
                c.Add( p );

                SqlParameter pOut0 = new SqlParameter( "@y", SqlDbType.Int );
                pOut0.Direction = ParameterDirection.Output;
                c.Add( pOut0 );

                SqlParameter pOut1 = new SqlParameter( "@d", SqlDbType.DateTime );
                pOut1.Direction = ParameterDirection.InputOutput;
                c.Add( pOut1 );

                SqlParameter pOut2 = new SqlParameter( "@s", SqlDbType.NVarChar, 64 );
                pOut2.Direction = ParameterDirection.Output;
                c.Add( pOut2 );

                p = new SqlParameter( "@z", SqlDbType.Int );
                c.Add( p );

                return cmd;
            }

            //[SqlProcedure( "CK.sStupidTest" )]
            public void CallStandardSP( ISqlCallContext ctx, int x, out int y, ref DateTime d, out string s, int z )
            {
                string spName = "CK.sStupidTest";

                using( SqlCommand cmd = new SqlCommand( spName ) )
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlParameterCollection c = cmd.Parameters;
                    SqlParameter p;

                    p = new SqlParameter( "@x", SqlDbType.Int );
                    p.Value = x;
                    c.Add( p );

                    SqlParameter pOut0 = new SqlParameter( "@y", SqlDbType.Int );
                    pOut0.Direction = ParameterDirection.Output;
                    y = default( int );
                    c.Add( pOut0 );

                    SqlParameter pOut1 = new SqlParameter( "@d", SqlDbType.DateTime );
                    pOut1.Direction = ParameterDirection.InputOutput;
                    pOut1.Value = d;
                    c.Add( pOut1 );

                    SqlParameter pOut2 = new SqlParameter( "@s", SqlDbType.NVarChar, 64 );
                    pOut2.Direction = ParameterDirection.Output;
                    s = default( string );
                    c.Add( pOut2 );

                    p = new SqlParameter( "@z", SqlDbType.Int );
                    p.Value = z;
                    c.Add( p );

                    cmd.Connection = ctx.Connection;
                    cmd.ExecuteNonQuery();

                    y = (int)pOut0.Value;
                    d = (DateTime)pOut1.Value;
                    s = (string)pOut2.Value;
                }
            }
        }

        [Test]
        public void ManualImplementation()
        {
            using( SqlManager m = new SqlManager( TestHelper.Monitor ) )
            {
                m.OpenFromConnectionString( ConnectionString, true ).Should().BeTrue( $"Unable to open or create test database on local server: {ConnectionString}." );
                var install = SqlHelper.SplitGoSeparator( File.ReadAllText( Path.Combine( TestHelper.TestProjectFolder, "Scripts/CallProcedureAutoImplementor.sql" ) ) );

                m.ExecuteScripts( install, TestHelper.Monitor )
                    .Should().BeTrue();

                SqlCallContext c = new SqlCallContext( m );
                ManualCall manual = new ManualCall();
                int y;
                DateTime d = DateTime.UtcNow;
                string s;
                manual.CallStandardSP( c, 3, out y, ref d, out s, 180 );

                Assert.That( s, Is.EqualTo( "x=3 z=180" ) );
                Assert.That( y, Is.EqualTo( 183 ) );
            }
        }

    }
}
