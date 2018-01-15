using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using SqlActorPackage.Basic;
using static CK.Testing.DBSetupTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests.ActorPackage
{
    public partial class DatabaseSetup
    {
        static void CheckCommandParamInjection( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Monitor.OpenTrace("CheckBasicPackageForCommandWrappers" ) )
            {
                var package = map.Default.Obtain<Package>();
                SimpleProcedureNaked( c, package );
                SimpleProcedureWithConnection( c, package );
                SimpleProcedureWithTransaction( c, package );
                SimpleProcedureWithTransactionAndConnection( c, package );
                SimpleProcedureWithTransactionAndNullConnection( c, package );
                SimpleProcedureWithNullTransactionAndConnection( c, package );
                SimpleProcedureWithNullTransactionAndNullConnection( c, package );
            }
        }

        private static void SimpleProcedureNaked( SqlManager c, Package package )
        {
            string result;
            using( var cmd = package.SimpleProcedureNaked( 3712, "Test", out result ) )
            {
                cmd.Connection = c.Connection;
                cmd.ExecuteNonQuery();
                cmd.Parameters["@Result"].Value.Should().Be( "Test - 3712" );
            }
        }

        private static void SimpleProcedureWithConnection( SqlManager c, Package package )
        {
            string result;
            using( var cmd = package.SimpleProcedureWithConnection( c.Connection, 78, "Test2", out result ) )
            {
                cmd.ExecuteNonQuery();
                cmd.Parameters["@Result"].Value.Should().Be( "Test2 - 78" );
            }
        }

        private static void SimpleProcedureWithTransaction( SqlManager c, Package package )
        {
            string result;
            using( var t = c.Connection.BeginTransaction() )
            using( var cmd = package.SimpleProcedureWithTransaction( t, 100, "Test3", out result ) )
            {
                cmd.Transaction.Should().BeSameAs( t );
                cmd.Connection.Should().BeSameAs( t.Connection );
                cmd.ExecuteNonQuery();
                cmd.Parameters["@Result"].Value.Should().Be( "Test3 - 100" );
                t.Commit();
            }
        }

        private static void SimpleProcedureWithTransactionAndConnection( SqlManager c, Package package )
        {
            string result;
            using( var t = c.Connection.BeginTransaction() )
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 100, t, "Test3", out result, c.Connection ) )
            {
                cmd.Transaction.Should().BeSameAs( t );
                cmd.Connection.Should().BeSameAs( t.Connection );
                cmd.ExecuteNonQuery();
                cmd.Parameters["@Result"].Value.Should().Be( "Test3 - 100" );
                t.Commit();
            }
        }

        private static void SimpleProcedureWithTransactionAndNullConnection( SqlManager c, Package package )
        {
            string result;
            using( var t = c.Connection.BeginTransaction() )
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 200, t, "Test4", out result, null ) )
            {
                cmd.Transaction.Should().BeSameAs( t );
                cmd.Connection.Should().BeSameAs( t.Connection );
                cmd.ExecuteNonQuery();
                cmd.Parameters["@Result"].Value.Should().Be( "Test4 - 200" );
                t.Commit();
            }
        }

        private static void SimpleProcedureWithNullTransactionAndConnection( SqlManager c, Package package )
        {
            string result;
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 300, null, "Test5", out result, c.Connection ) )
            {
                cmd.Transaction.Should().BeNull();
                cmd.Connection.Should().BeSameAs( c.Connection );
                cmd.ExecuteNonQuery();
                cmd.Parameters["@Result"].Value.Should().Be( "Test5 - 300" );
            }
        }

        private static void SimpleProcedureWithNullTransactionAndNullConnection( SqlManager c, Package package )
        {
            string result;
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 400, null, "Test6", out result, null ) )
            {
                cmd.Transaction.Should().BeNull();
                cmd.Connection.Should().BeNull();
            }
        }

    }
}
