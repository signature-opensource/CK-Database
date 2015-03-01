#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests\ActorPackage\DatabaseSetup.CommandParamInjection.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using NUnit.Framework;
using SqlActorPackage.Basic;

namespace CK.SqlServer.Setup.Engine.Tests.ActorPackage
{
    public partial class DatabaseSetup
    {
        static void CheckCommandParamInjection( SqlManager c, IStObjMap map )
        {
            using( TestHelper.ConsoleMonitor.OpenTrace().Send( "CheckBasicPackageForCommandWrappers" ) )
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
                c.Connection.ExecuteNonQuery( cmd );
                Assert.That( cmd.Parameters["@Result"].Value, Is.EqualTo( "Test - 3712" ) );
            }
        }

        private static void SimpleProcedureWithConnection( SqlManager c, Package package )
        {
            string result;
            using( var cmd = package.SimpleProcedureWithConnection( c.Connection.InternalConnection, 78, "Test2", out result ) )
            {
                cmd.ExecuteNonQuery();
                Assert.That( cmd.Parameters["@Result"].Value, Is.EqualTo( "Test2 - 78" ) );
            }
        }

        private static void SimpleProcedureWithTransaction( SqlManager c, Package package )
        {
            string result;
            using( var t = c.Connection.InternalConnection.BeginTransaction() )
            using( var cmd = package.SimpleProcedureWithTransaction( t, 100, "Test3", out result ) )
            {
                Assert.That( cmd.Transaction, Is.SameAs( t ) );
                Assert.That( cmd.Connection, Is.SameAs( t.Connection ) );
                cmd.ExecuteNonQuery();
                Assert.That( cmd.Parameters["@Result"].Value, Is.EqualTo( "Test3 - 100" ) );
                t.Commit();
            }
        }

        private static void SimpleProcedureWithTransactionAndConnection( SqlManager c, Package package )
        {
            string result;
            using( var t = c.Connection.InternalConnection.BeginTransaction() )
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 100, t, "Test3", out result, c.Connection.InternalConnection ) )
            {
                Assert.That( cmd.Transaction, Is.SameAs( t ) );
                Assert.That( cmd.Connection, Is.SameAs( t.Connection ) );
                cmd.ExecuteNonQuery();
                Assert.That( cmd.Parameters["@Result"].Value, Is.EqualTo( "Test3 - 100" ) );
                t.Commit();
            }
        }

        private static void SimpleProcedureWithTransactionAndNullConnection( SqlManager c, Package package )
        {
            string result;
            using( var t = c.Connection.InternalConnection.BeginTransaction() )
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 200, t, "Test4", out result, null ) )
            {
                Assert.That( cmd.Transaction, Is.SameAs( t ) );
                Assert.That( cmd.Connection, Is.SameAs( t.Connection ) );
                cmd.ExecuteNonQuery();
                Assert.That( cmd.Parameters["@Result"].Value, Is.EqualTo( "Test4 - 200" ) );
                t.Commit();
            }
        }

        private static void SimpleProcedureWithNullTransactionAndConnection( SqlManager c, Package package )
        {
            string result;
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 300, null, "Test5", out result, c.Connection.InternalConnection ) )
            {
                Assert.That( cmd.Transaction, Is.Null );
                Assert.That( cmd.Connection, Is.SameAs( c.Connection.InternalConnection ) );
                cmd.ExecuteNonQuery();
                Assert.That( cmd.Parameters["@Result"].Value, Is.EqualTo( "Test5 - 300" ) );
            }
        }

        private static void SimpleProcedureWithNullTransactionAndNullConnection( SqlManager c, Package package )
        {
            string result;
            using( var cmd = package.SimpleProcedureWithConnectionAndTransaction( 400, null, "Test6", out result, null ) )
            {
                Assert.That( cmd.Transaction, Is.Null );
                Assert.That( cmd.Connection, Is.Null );
            }
        }

    }
}
