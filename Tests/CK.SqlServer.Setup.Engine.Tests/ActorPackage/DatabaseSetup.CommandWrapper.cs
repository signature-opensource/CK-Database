#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests\ActorPackage\DatabaseSetup.CommandWrapper.cs) is part of CK-Database. 
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
        static void CheckCommandWrapper( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Monitor.OpenTrace( "CheckCommandWrapper" ) )
            {
                var package = map.Default.Obtain<Package>();
                SimplestScalar( c, package );
                SimplestScalarWithHome( package );
                SimplestScalarWithBaseOfTheHome( package );
                SimplestScalarWithHomeInterface( package );
                SimplestScalarWithTimeout( package );
                SimplestScalarWithParams( package );
            }
        }

        static void SimplestScalar( SqlManager c, Package package )
        {
            using( var cmd = package.SimplestScalar( c.Connection, 3712, "Test" ) )
            {
                Assert.That( cmd.Execute(), Is.EqualTo( "Test - 3712" ) );
            }
        }

        static void SimplestScalarWithHome( Package package )
        {
            using( var cmd = package.SimplestScalarWithAccessToHome( 371, "Test2" ) )
            {
                Assert.That( cmd.Execute(), Is.EqualTo( "Test2 - 371" ) );
            }
        }

        static void SimplestScalarWithBaseOfTheHome( Package package )
        {
            using( var cmd = package.SimplestScalarAccessToABaseOfTheHome( 37, "Test3" ) )
            {
                Assert.That( cmd.Execute(), Is.EqualTo( "Test3 - 37" ) );
            }
        }

        static void SimplestScalarWithHomeInterface( Package package )
        {
            using( var cmd = package.SimplestScalarAccessToInterfaceHome( 3, "Test4" ) )
            {
                Assert.That( cmd.Execute(), Is.EqualTo( "Test4 - 3" ) );
            }
        }

        static void SimplestScalarWithTimeout( Package package )
        {
            using( var cmd = package.SimplestScalarWithTimeout( 1, "Test5", 3712 ) )
            {
                Assert.That( cmd.Timeout, Is.EqualTo( 3712 ) );
                Assert.That( cmd.Execute(), Is.EqualTo( "Test5 - 1" ) );
            }
        }

        static void SimplestScalarWithParams( Package package )
        {
            using( var cmd = package.SimplestScalarWithLogParams( "Msg1", 0, "Test6", 6585, "Msg2" ) )
            {
                Assert.That( cmd.Timeout, Is.EqualTo( 6585 ) );
                Assert.That( cmd.LogMessage1, Is.EqualTo( "Msg1" ) );
                Assert.That( cmd.LogMessage2, Is.EqualTo( "Msg2" ) );
                Assert.That( cmd.Execute(), Is.EqualTo( "Test6 - 0" ) );
            }
        }

    }
}
