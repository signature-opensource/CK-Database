using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using SqlActorPackage.Basic;

namespace CK.SqlServer.Setup.Engine.Tests.ActorPackage
{
    public partial class DatabaseSetup
    {
        static void CheckSqlCallContext( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Monitor.OpenTrace( "CheckSqlCallContext" ) )
            {
                var package = map.Default.Obtain<Package>();
                CallWithAuthContext( package );
                CallWithAuthContextWithoutOutSignature( package );
            }
        }

        private static void CallWithAuthContext( Package package )
        {
            using( var ctx = new Package.BasicAuthContext() { ActorId = 2145 } )
            {
                string signatureResult;
                using( var c = package.CallWithAuth( ctx, 1, "Name", out signatureResult ) )
                {
                    Assert.That( c.Call(), Is.EqualTo( "2145: Name - 1" ) );
                }
            }
        }

        private static void CallWithAuthContextWithoutOutSignature( Package package )
        {
            using( var ctx = new Package.BasicAuthContext() { ActorId = 21 } )
            using( var c = package.CallWithAuth( ctx, 2, "Name2" ) )
            {
                Assert.That( c.Call(), Is.EqualTo( "21: Name2 - 2" ) );
            }
        }
    }
}
