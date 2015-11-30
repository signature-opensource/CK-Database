using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setupable.Engine.Tests
{
    [TestFixture]
    public class DefaultContextLocNamingTests
    {
        [Test]
        public void parsing_multiple_forms()
        {
            CheckParse( "", null, null, "" );
            CheckParse( "^", null, "", "" );
            CheckParse( "[]^", "", "", "" );
            CheckParse( "[H]loc^", "H", "loc", "" );
            CheckParse( "[H]loc^N", "H", "loc", "N" );

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
        public void combining_null_or_empty_names_with_baseName_gives_the_namespace_of_the_baseName()
        {
            CheckCombineName( "", "", expected: "" );
            CheckCombineName( "", "B", expected: "" );
            CheckCombineName( "", "B.C", expected: "B" );

            CheckCombineName( null, "", expected: "" );
            CheckCombineName( null, "B", expected: "" );
            CheckCombineName( null, "B.C", expected: "B" );
        }

        [Test]
        public void combining_null_or_empty_names_with_namespace_gives_the_namespace()
        {
            CheckCombineNamespace( "", "", expected: "" );
            CheckCombineNamespace( "", "B", expected: "B" );
            CheckCombineNamespace( "", "B.C", expected: "B.C" );

            CheckCombineNamespace( null, "", expected: "" );
            CheckCombineNamespace( null, "B", expected: "B" );
            CheckCombineNamespace( null, "B.C", expected: "B.C" );
        }

        [Test]
        public void combining_pure_dot_name_always_gives_the_baseName_be_it_a_namespace_or_not()
        {
            CheckCombineName( ".", "", expected: "" );
            CheckCombineNamespace( ".", "", expected: "" );
            CheckCombineName( ".", "B", expected: "B", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".", "B", expected: "B" );
            CheckCombineName( ".", "B.n", expected: "B.n", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".", "B.n", expected: "B.n" );
            CheckCombineName( ".", "B.n.s", expected: "B.n.s", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".", "B.n.s", expected: "B.n.s" );
        }

        [Test]
        public void combining_dot_starting_name_considers_the_baseName_as_a_namespace()
        {
            CheckCombineName( ".N", "", expected: "N" );
            CheckCombineNamespace( ".N", "", expected: "N" );
            CheckCombineName( ".N", "B", expected: "B.N", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".N", "B", expected: "B.N" );
            CheckCombineName( ".N", "B.n", expected: "B.n.N", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".N", "B.n", expected: "B.n.N" );
            CheckCombineName( ".N", "B.n.s", expected: "B.n.s.N", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".N", "B.n.s", expected: "B.n.s.N" );
        }

        [Test]
        public void combining_names_that_goes_up_in_the_path_considers_the_baseName_as_a_namespace()
        {
            CheckCombineName( ".~", "A", expected: "", getNamespaceOnBaseNameGivesTheSame: false  );
            CheckCombineNamespace( ".~", "A", expected: "" );
            CheckCombineName( ".~.", "A", expected: "", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".~.", "A", expected: "" );

            CheckCombineName( ".~", "A.B", expected: "A", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineName( ".~.", "A.B", expected: "A", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".~", "A.B", expected: "A" );
            CheckCombineNamespace( ".~.", "A.B", expected: "A" );

            CheckCombineName( ".~.X", "A.B", expected: "A.X", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".~.X", "A.B", expected: "A.X" );

            CheckCombineName( ".~.X", "A.B.C", expected: "A.B.X", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".~.X", "A.B.C", expected: "A.B.X" );

            CheckCombineName( ".~.X", "A.B.C.D", expected: "A.B.C.X", getNamespaceOnBaseNameGivesTheSame: false );
            CheckCombineNamespace( ".~.X", "A.B.C.D", expected: "A.B.C.X" );
        }


        [Test]
        public void combining_with_baseName_as_a_name_is_the_same_as_combinig_to_the_namespace_of_the_baseName()
        {
            CheckCombineName( "R.S", "", expected: "R.S" );
            CheckCombineName( "R.S", "B", expected: "R.S" );
            CheckCombineName( "R.S", "B.n", expected: "B.R.S" );
            CheckCombineName( "R.S", "B.n.s", expected: "B.n.R.S" );
        }

        [Test]
        public void combining_rooted_names_ignores_baseName_or_namespace()
        {
            CheckCombineName( "~R", "", expected: "R" );
            CheckCombineNamespace( "~R", "", expected: "R" );
            CheckCombineName( "~R", "B", expected: "R" );
            CheckCombineNamespace( "~R", "B", expected: "R" );
            CheckCombineName( "~R", "B.n", expected: "R" );
            CheckCombineNamespace( "~R", "B.n", expected: "R" );
            CheckCombineName( "~R", "B.n.s", expected: "R" );
            CheckCombineNamespace( "~R", "B.n.s", expected: "R" );

            CheckCombineName( "~R.S", "", expected: "R.S" );
            CheckCombineNamespace( "~R.S", "", expected: "R.S" );
            CheckCombineName( "~R.S", "B", expected: "R.S" );
            CheckCombineNamespace( "~R.S", "B", expected: "R.S" );
            CheckCombineName( "~R.S", "B.n", expected: "R.S" );
            CheckCombineNamespace( "~R.S", "B.n", expected: "R.S" );
            CheckCombineName( "~R.S", "B.n.s", expected: "R.S" );
            CheckCombineNamespace( "~R.S", "B.n.s", expected: "R.S" );
        }

        [Test]
        public void combining_that_results_in_a_path_above_the_base_path_is_always_an_error()
        {
            CheckCombineName( ".~", "", expected: null );
            CheckCombineName( ".~.", "", expected: null );

            CheckCombineNamespace( ".~", "", expected: null );
            CheckCombineNamespace( ".~.", "", expected: null );

            CheckCombineName( ".~.~", "A", expected: null );
            CheckCombineName( ".~.~.", "A", expected: null );
            CheckCombineNamespace( ".~.~", "A", expected: null );
            CheckCombineNamespace( ".~.~.", "A", expected: null );

            CheckCombineName( ".~.~.~", "A.B", expected: null );
            CheckCombineName( ".~.~.~.", "A.B", expected: null );
            CheckCombineNamespace( ".~.~.~", "A.B", expected: null );
            CheckCombineNamespace( ".~.~.~.", "A.B", expected: null );
        }

        void CheckCombineName( string name, string baseName, string expected, bool getNamespaceOnBaseNameGivesTheSame = true )
        {
            string result;
            Assert.That( DefaultContextLocNaming.TryCombineNamePart( name, baseName, out result, false ) || result == null );
            Assert.That( result, Is.EqualTo( expected ) );
            if( getNamespaceOnBaseNameGivesTheSame )
            {
                Assert.That( DefaultContextLocNaming.TryCombineNamePart( name, DefaultContextLocNaming.GetNamespace( baseName ), out result, true ) || result == null );
                Assert.That( result, Is.EqualTo( expected ) );
            }
        }

        void CheckCombineNamespace( string name, string baseName, string expected )
        {
            string result;
            Assert.That( DefaultContextLocNaming.TryCombineNamePart( name, baseName, out result, true ) || result == null );
            Assert.That( result, Is.EqualTo( expected ) );
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
            Assert.Throws<CKException>( () => DefaultContextLocNaming.Resolve( input, curContext, curLoc ) );
            Assert.Throws<CKException>( () => DefaultContextLocNaming.Resolve( "?" + input, 1, curContext, curLoc ) );
            Assert.Throws<CKException>( () => DefaultContextLocNaming.Resolve( "??" + input, 2, curContext, curLoc ) );
            Assert.Throws<CKException>( () => DefaultContextLocNaming.Resolve( input + "pouf", 0, input.Length, curContext, curLoc ) );
            Assert.Throws<CKException>( () => DefaultContextLocNaming.Resolve( "pif" + input + "pouf", 3, input.Length, curContext, curLoc ) );
        }

        [Test]
        public void CheckAddNamePrefix()
        {
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "CK.UserHome", "Model." ), Is.EqualTo( "Model.CK.UserHome" ) );
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "CK.UserHome-Local", "Model." ), Is.EqualTo( "Model.CK.UserHome-Local" ) );
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "^CK.UserHome", "Model." ), Is.EqualTo( "^Model.CK.UserHome" ) );
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "^CK.UserHome-Local", "Model." ), Is.EqualTo( "^Model.CK.UserHome-Local" ) );
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "[]CK.UserHome", "Model." ), Is.EqualTo( "[]Model.CK.UserHome" ) );
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "[]CK.UserHome-Local", "Model." ), Is.EqualTo( "[]Model.CK.UserHome-Local" ) );
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "[]-db^CK.UserHome", "Model." ), Is.EqualTo( "[]-db^Model.CK.UserHome" ) );
            Assert.That( DefaultContextLocNaming.AddNamePrefix( "[]-db^CK.UserHome-Local", "Model." ), Is.EqualTo( "[]-db^Model.CK.UserHome-Local" ) );
        }
    }
}
