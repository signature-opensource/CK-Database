using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace CK.SqlServer.Tests
{

    public interface ISqlCallContext
    {
        IActivityMonitor Logger { get; }

        SqlConnectionProvider Connection { get; }

        bool SetAmbientValue( SqlParameter p );
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
            get { return TestHelper.ConsoleMonitor; }
        }

        public SqlConnectionProvider Connection
        {
            get { return _m.Connection; }
        }

        public bool SetAmbientValue( SqlParameter p )
        {
            return false;
        }
    }

    [TestFixture]
    public class CallProcedureAutoImplementor
    {

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

            //[SqlAutoImplement( "CK.sChoucroute" )]
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

                    ctx.Connection.ExecuteNonQuery( cmd );

                    y = (int)pOut0.Value;
                    d = (DateTime)pOut1.Value;
                    s = (string)pOut2.Value;
                }
            }
        }


        [Test]
        public void ManualImplementation()
        {
            using( SqlManager m = new SqlManager() )
            {
                Assert.That( m.OpenOrCreate( ".", "CKSqlServerTests" ), "Unable to open or create CKSqlServerTests database on local server." );
                var install = SqlHelper.SplitGoSeparator( File.ReadAllText( TestHelper.GetScriptsFolder( "ManualImplementation.sql" ) ) );
                m.ExecuteScripts( install, TestHelper.ConsoleMonitor );

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
