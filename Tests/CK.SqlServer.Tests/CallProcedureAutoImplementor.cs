//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using NUnit.Framework;
//using CK.Core;
//using System.Data.SqlClient;
//using System.Data;
//using System.IO;

//namespace CK.SqlServer.Tests
//{

//    public interface ISqlCallContext
//    {
//        IActivityLogger Logger { get; }

//        SqlConnectionProvider Connection { get; }

//        bool SetAmbientValue( SqlParameter p );
//    }

//    public class SqlCallContext : ISqlCallContext
//    {
//        readonly SqlManager _m;

//        public SqlCallContext( SqlManager m )
//        {
//            _m = m;
//        }

//        public IActivityLogger Logger
//        {
//            get { return TestHelper.Logger; }
//        }

//        public SqlConnectionProvider Connection
//        {
//            get { return _m.Connection; }
//        }

//        public bool SetAmbientValue( SqlParameter p )
//        {
//            return false;
//        }
//    }

//    [TestFixture]
//    public class CallProcedureAutoImplementor
//    {

//        class ManualCall
//        {

//            public void CallStandardSP( ISqlCallContext ctx, int x, out int y, ref DateTime d, out string s, int z )
//            {
//                string spName = "CK.sStupidTest";
//                object[] inputP = new object[] { x, d, z }; 
//                object[] output = new object[3];

//                GenericStandardSPCall( ctx, spName, inputP, output );

//                y = (int)output[0];
//                d = (DateTime)output[1];
//                s = (string)output[2];
//            }

//            private static void GenericStandardSPCall( ISqlCallContext ctx, string spName, object[] inputP, object[] output )
//            {
//                SqlCommand cmd = new SqlCommand( spName );
//                cmd.CommandType = CommandType.StoredProcedure;
//                SqlCommandBuilder.DeriveParameters( cmd );
//                int i = 0;
//                foreach( SqlParameter p in cmd.Parameters )
//                {
//                    if( p.Direction == ParameterDirection.Input )
//                    {
//                        if( !ctx.SetAmbientValue( p ) )
//                        {
//                            p.Value = inputP[i++] ?? DBNull.Value;
//                        }
//                    }
//                    else if( p.Direction == ParameterDirection.InputOutput )
//                    {
//                        p.Value = inputP[i++] ?? DBNull.Value;
//                    }
//                }
//                ctx.Connection.ExecuteNonQuery( cmd );
//                if( output.Length > 0 )
//                {
//                    i = 0;
//                    foreach( SqlParameter p in cmd.Parameters )
//                    {
//                        if( p.Direction == ParameterDirection.Output )
//                        {
//                            output[i++] = p.Value;
//                        }
//                    }
//                }
//            }
//        }


//        [Test]
//        public void ManualImplementation()
//        {
//            using( SqlManager m = new SqlManager() )
//            {
//                Assert.That( m.OpenOrCreate( ".", "CKSqlServerTests" ), "Unable to open or create CKSqlServerTests database on local server." );
//                var install = SqlHelper.SplitGoSeparator( File.ReadAllText( TestHelper.GetScriptsFolder( "ManualImplementation.sql" ) ) );
//                m.ExecuteScripts( install, TestHelper.Logger );

//                SqlCallContext c = new SqlCallContext( m );
//                ManualCall manual = new ManualCall();
//                int y;
//                DateTime d = DateTime.UtcNow;
//                string s;
//                manual.CallStandardSP( c, 3, out y, ref d, out s, 180 ); 
//            }
//        }

//    }
//}
