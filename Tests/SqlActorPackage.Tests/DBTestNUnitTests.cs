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
                a.Database.AssertAllCKCoreInvariant();
                try
                {
                    a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly', 'from sys.tables';" );
                    a.Database.AssertAllCKCoreInvariant();
                }
                catch( AssertionException ex )
                {
                    TestHelper.Monitor.Trace().Send( ex.Message );
                    Assert.That( ex.Message, Is.EqualTo( @"
InvariantKey    | CountSelect                              | RunStatus
----------------------------------------------------------------------
FakeForTestOnly | select @Count = count(*) from sys.tables | Failed   
".Substring( 2 ) ) );
                }
            }
            finally
            {
                a.Database.ExecuteNonQuery( "exec CKCore.sInvariantRegister 'FakeForTestOnly', null;" );
            }
        }
    }

}
