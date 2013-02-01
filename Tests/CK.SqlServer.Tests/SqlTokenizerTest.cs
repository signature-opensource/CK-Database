using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.SqlServer;

namespace CK.Javascript.Tests
{
    [TestFixture]
    public class SqlTokenizerTest
    {
        [Test]
        public void BasicToken()
        {
            {
                SqlTokeniser p = new SqlTokeniser();
                p.SkipComments = false;
                p.Reset( "" );
                Assert.That( p.IsEndOfInput );
            }
            {
                SqlTokeniser p = new SqlTokeniser();
                p.SkipComments = false;
                Assert.That( SqlTokeniser.Explain( SqlTokeniserToken.Integer ), Is.EqualTo( "42" ) );

                string s = " ( x , z ) { ( x != z || x && z % x - x >>> z >> z << x | z & x ^ z -- ...\r\n = x @param select ) x + ( z * 42 ) / 42 ; } == += -= >>= >>>= x % z %= x z x ! z ~= x |= z &= x <<= z ^= x /= z *= x %=";
                p.Reset( s );
                string recompose = "";
                while( !p.IsEndOfInput )
                {
                    recompose += " " + SqlTokeniser.Explain( p.CurrentToken );
                    p.Forward();
                }
                s = s.Replace( "x", "identifier" )
                    .Replace( "z", "identifier" )
                    .Replace( "select", "keyword" )
                    .Replace( "@param", "identifier" );

                Assert.That( recompose, Is.EqualTo( s ) );
            }
        }
    }
}
