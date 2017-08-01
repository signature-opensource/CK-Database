using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlTransform.Tests
{
    [TestFixture]
    public class IntermediateTransformationTests
    {
        [Test]
        public void vDependent_requires_an_intermediate_concretisation_of_vBase()
        {
            var p1 = TestHelper.StObjMap.Default.Obtain<CKLevel2.IntermediateTransformation.Package1>();
            var p2 = TestHelper.StObjMap.Default.Obtain<CKLevel2.IntermediateTransformation.Package2>();
            var p3 = TestHelper.StObjMap.Default.Obtain<CKLevel2.IntermediateTransformation.Package3>();
            using( var ctx = new SqlStandardCallContext() )
            {
                Assert.That( p1.ReadViewBase( ctx ).Count, Is.GreaterThan( 0 ) );
                var idAndNames = p2.ReadViewBase( ctx );
                Assert.That( idAndNames.Count, Is.GreaterThan( 0 ) );
                Assert.That( idAndNames.FindIndex( kv => kv.Value == "tSystem" ) >= 0 );
                var idNameAndTypes = p3.ReadViewBase( ctx );
                Assert.That( idNameAndTypes.Count, Is.GreaterThan( 0 ) );
                Assert.That( idNameAndTypes.FindIndex( t => t.Item2 == "tSystem" && t.Item3 == "U" ) >= 0 );
            }
        }
    }
}
