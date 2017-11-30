using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System.Diagnostics;
using NUnit.Framework.Constraints;
using System.Data.SqlClient;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class DBTestNUnitTests
    {
        [Test]
        public void CKCore_invariants_checking()
        {
            var a = TestHelper.StObjMap.Default.Obtain<ActorHome>();
            try
            {
                a.Database.GetCKCoreInvariantsViolations();
                a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly', 'from sys.tables';" );
                try
                {
                    a.Database.GetCKCoreInvariantsViolations( "FakeForTestOnly" );
                }
                catch( AssertionException ex )
                {
                    TestHelper.Monitor.Trace( ex.Message );
                    Assert.That( ex.Message, Is.EqualTo( @"
InvariantKey    | CountSelect                              | RunStatus
----------------------------------------------------------------------
FakeForTestOnly | select @Count = count(*) from sys.tables | Failed   
".Substring( 2 ) ) );
                }
                try
                {
                    a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly', null;" );
                    a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly2', 'from sys.tables';" );
                    a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly3', 'from sys.XXXXXX';" );
                    Assert.Throws<SqlException>( () => a.Database.GetCKCoreInvariantsViolations( "FakeForTestOnly" ) );
                    a.Database.GetCKCoreInvariantsViolations( "FakeForTestOnly2" );
                }
                catch( AssertionException ex )
                {
                    TestHelper.Monitor.Trace( ex.Message );
                    Assert.That( ex.Message, Is.EqualTo( @"
InvariantKey     | CountSelect                              | RunStatus
-----------------------------------------------------------------------
FakeForTestOnly2 | select @Count = count(*) from sys.tables | Failed   
".Substring( 2 ) ) );
                }
                try
                {
                    a.Database.GetCKCoreInvariantsViolations();
                }
                catch( AssertionException ex )
                {
                    TestHelper.Monitor.Trace( ex.Message );
                    Assert.That( ex.Message, Is.EqualTo( @"
InvariantKey     | CountSelect                              | RunStatus  
-------------------------------------------------------------------------
FakeForTestOnly2 | select @Count = count(*) from sys.tables | Failed     
FakeForTestOnly3 | select @Count = count(*) from sys.XXXXXX | Fatal Error
".Substring( 2 ) ) );
                }
            }
            finally
            {
                a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly', null;" );
                a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly2', null;" );
                a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly3', null;" );
            }
        }
    }

}
