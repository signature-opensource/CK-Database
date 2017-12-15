using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqlTransform.Tests
{
    [TestFixture]
    public class TransformTests
    {

        internal static SqlCommand GeneratedCreateCommand()
        {
            SqlCommand command = new SqlCommand("CK.sSimpleReplaceTest")
            {
                CommandType = CommandType.StoredProcedure
            };
            SqlParameterCollection parameters = command.Parameters;
            SqlParameter parameter1 = new SqlParameter("@TextParam", SqlDbType.NVarChar, 0x80)
            {
                Direction = ParameterDirection.InputOutput
            };
            parameters.Add(parameter1);
            parameters.Add(new SqlParameter("@Added", SqlDbType.Int));
            return command;
        }

        public string SimpleReplaceTestGenerated(ISqlCallContext context1, string text1, string extraConnectionString)
        {
            SqlCommand command;
            SqlParameterCollection parameters = (command = GeneratedCreateCommand()).Parameters;
            parameters[0].Value = (object)text1 ?? DBNull.Value;
            parameters[1].Value = 0;
            context1.Executor.ExecuteNonQuery(extraConnectionString, (SqlCommand)command);
            return parameters[0].Value == DBNull.Value ? (string)null : (string)parameters[0].Value;
        }

        [Test]
        public void calling_SimpleReplaceTest_method()
        {
            var p = TestHelper.StObjMap.Default.Obtain<CKLevel0.Package>();
            using( var ctx = new SqlStandardCallContext() )
            {
                SimpleReplaceTestGenerated(ctx, "Pouf!", p.Database.ConnectionString);
                
                var aFromType = typeof(string).GetTypeInfo().Assembly;

                string s = p.SimpleReplaceTest( ctx, "Hello!" );
                Assert.That( s, Is.EqualTo( "Return: Hello! 0" ) );
            }
        }

        [Test]
        public void calling_SimpleTransformTest_method()
        {
            var b = CK.Testing.BasicTestHelper.TestHelper;
            var p = CK.Testing.StObjMapTestHelper.TestHelper.StObjMap.Default.Obtain<CKLevel0.Package>();
            var p2 = CK.Testing.StObjMapTestHelper.TestHelper.StObjMap.Default.Obtain<CKLevel2.Package>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string s;
                s = p.SimpleTransormTest( ctx );
                Assert.That( s, Is.EqualTo( "Yes! 0" ) );
                s = p2.SimpleTransformTest( ctx, "unused", 3712 );
                Assert.That( s, Is.EqualTo( "Yes! 3712" ) );
            }
        }

        [Test]
        public void calling_SimplY4TemplateTest_method()
        {
            var p = TestHelper.StObjMap.Default.Obtain<CKLevel0.Package>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string s = p.SimplY4TemplateTest( ctx );
                Assert.That( s, Does.Match( @"HashCode = \d+" ) );
            }
        }
    }
}
