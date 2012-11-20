using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests
{
    [TestFixture]
    public class DefaultContextLocNamingTests
    {
        [Test]
        public void Parse()
        {
            CheckParse( "CK.fTest", null, null, "CK.fTest" );
            CheckParse( "[]CK.fTest", "", null, "CK.fTest" );
            CheckParse( "[]^CK.fTest", "", "", "CK.fTest" );
            CheckParse( "^CK.fTest", null, "", "CK.fTest" );
            CheckParse( "[H]^CK.fTest", "H", "", "CK.fTest" );
            
            CheckParse( "[H]db^CK.fTest", "H", "db", "CK.fTest" );
            CheckParse( "[H]-db^CK.fTest", "H", "-db", "CK.fTest" );
            CheckParse( "[H]--db^CK.fTest", "H", "--db", "CK.fTest" );
            CheckParse( "[H]---srv-db^CK.fTest", "H", "---srv-db", "CK.fTest" );

            CheckParse( "srv-db^CK.fTest", null, "srv-db", "CK.fTest" );
            CheckParse( "[H]srvPrd-db^CK.fTest", "H", "srvPrd-db", "CK.fTest" );
            CheckParse( "[]srvPrd-db^CK.fTest", "", "srvPrd-db", "CK.fTest" );
            CheckParse( "", null, null, "" );
            CheckParse( "^", null, "", "" );
            CheckParse( "[]^", "", "", "" );
            CheckParse( "[H]loc^", "H", "loc", "" );
        }

        void CheckParse( string input, string expectedContext, string expectedLocation, string expectedName )
        {
            CheckParseWhole( input, expectedContext, expectedLocation, expectedName );
            CheckParseSub( input, expectedContext, expectedLocation, expectedName );
        }

        private static void CheckParseWhole( string input, string expectedContext, string expectedLocation, string expectedName )
        {
            string c, l, n;
            Assert.That( DefaultContextLocNaming.TryParse( input, out c, out l, out n ) );
            CheckOutput( expectedContext, expectedLocation, expectedName, c, l, n );
        }

        private static void CheckParseSub( string input, string expectedContext, string expectedLocation, string expectedName )
        {
            string c, l, n;
            Assert.That( DefaultContextLocNaming.TryParse( "?" + input, 1, out c, out l, out n ) );
            CheckOutput( expectedContext, expectedLocation, expectedName, c, l, n );
            Assert.That( DefaultContextLocNaming.TryParse( "??" + input, 2, out c, out l, out n ) );
            CheckOutput( expectedContext, expectedLocation, expectedName, c, l, n );
            Assert.That( DefaultContextLocNaming.TryParse( input + "pouf", 0, input.Length, out c, out l, out n ) );
            CheckOutput( expectedContext, expectedLocation, expectedName, c, l, n );
            Assert.That( DefaultContextLocNaming.TryParse( "pif" + input + "pouf", 3, input.Length, out c, out l, out n ) );
            CheckOutput( expectedContext, expectedLocation, expectedName, c, l, n );
        }

        private static void CheckOutput( string expectedContext, string expectedLocation, string expectedName, string c, string l, string n )
        {
            Assert.That( c, Is.EqualTo( expectedContext ) );
            Assert.That( l, Is.EqualTo( expectedLocation ) );
            Assert.That( n, Is.EqualTo( expectedName ) );
        }

        [Test]
        public void Resolve()
        {
            CheckResolve( "CK.fTest", null, null, "CK.fTest" );
            CheckResolve( "CK.fTest", "", null, "[]CK.fTest" );
            CheckResolve( "CK.fTest", "", "", "[]^CK.fTest" );
            CheckResolve( "CK.fTest", null, "", "^CK.fTest" );
            CheckResolve( "CK.fTest", "H", "db", "[H]db^CK.fTest" );
            CheckResolve( "^CK.fTest", "H", "nimp", "[H]^CK.fTest" );
            CheckResolve( "srv-db^CK.fTest", "H", "nimp", "[H]srv-db^CK.fTest" );
            CheckResolve( "[]srv-db^CK.fTest", "nimp", "nimp", "[]srv-db^CK.fTest" );
            CheckResolve( "[]^CK.fTest", "nimp", "nimp", "[]^CK.fTest" );
            CheckResolve( "^CK.fTest", "H", "nimp", "[H]^CK.fTest" );
            CheckResolve( "-^CK.fTest", null, "srv-db", "srv^CK.fTest" );
            CheckResolve( "--srv2-db3^CK.fTest", "", "sys-srv-db", "[]sys-srv2-db3^CK.fTest" );
            CheckResolve( "---srv2-db3^CK.fTest", "A", null, "[A]---srv2-db3^CK.fTest" );

            CheckFormatException( "-^H", "nimp", "" );
            CheckFormatException( "--^H", "nimp", "oneLoc" );
        }

        void CheckResolve( string input, string curContext, string curLoc, string expectedResult )
        {
            Assert.That( DefaultContextLocNaming.Resolve( input, curContext, curLoc ), Is.EqualTo( expectedResult ) );
            Assert.That( DefaultContextLocNaming.Resolve( "?" + input, 1, curContext, curLoc ), Is.EqualTo( "?" + expectedResult ) );
            Assert.That( DefaultContextLocNaming.Resolve( "??" + input, 2, curContext, curLoc ), Is.EqualTo( "??" + expectedResult ) );
            Assert.That( DefaultContextLocNaming.Resolve( input + "pouf", 0, input.Length, curContext, curLoc ), Is.EqualTo( expectedResult + "pouf" ) );
            Assert.That( DefaultContextLocNaming.Resolve( "pif" + input + "pouf", 3, input.Length, curContext, curLoc ), Is.EqualTo( "pif" + expectedResult + "pouf" ) );
        }

        void CheckFormatException( string input, string curContext, string curLoc )
        {
            Assert.Throws<FormatException>( () => DefaultContextLocNaming.Resolve( input, curContext, curLoc ) );
            Assert.Throws<FormatException>( () => DefaultContextLocNaming.Resolve( "?" + input, 1, curContext, curLoc ) );
            Assert.Throws<FormatException>( () => DefaultContextLocNaming.Resolve( "??" + input, 2, curContext, curLoc ) );
            Assert.Throws<FormatException>( () => DefaultContextLocNaming.Resolve( input + "pouf", 0, input.Length, curContext, curLoc ) );
            Assert.Throws<FormatException>( () => DefaultContextLocNaming.Resolve( "pif" + input + "pouf", 3, input.Length, curContext, curLoc ) );
        }

    }
}
