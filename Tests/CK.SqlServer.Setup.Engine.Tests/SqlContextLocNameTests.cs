using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class SqlContextLocNameTests
    {
        [TestCase( "[]db^CKonstC.CCC([]db^CKonstV.vVVV)", "vVVV,CKonstV.vVVV,CCC(vVVV),CCC(CKonstV.vVVV),CCC([]db^CKonstV.vVVV),CKonstC.CCC(vVVV),CKonstC.CCC(CKonstV.vVVV),CKonstC.CCC([]db^CKonstV.vVVV),db^CKonstC.CCC(vVVV),db^CKonstC.CCC(CKonstV.vVVV),db^CKonstC.CCC([]db^CKonstV.vVVV),[]db^CKonstV.vVVV,CCC([]db^CKonstV.vVVV),CKonstC.CCC([]db^CKonstV.vVVV),[]db^CKonstC.CCC([]db^CKonstV.vVVV)" )]
        public void transformer_name_generates_ressource_names( string name, string expected )
        {
            var n = new SqlContextLocName( name );
            var containerName = new SqlContextLocName( name );
            containerName.TransformArg = null;

            var gen = n.GetResourceNameCandidates( containerName ).ToArray();
            var exp = expected.Split( ',' );
            Assert.That( gen.SequenceEqual( exp ) );
        }
    }
}
