using NUnit.Framework;
using CK.Setup;
using System;

namespace CK.Setupable.Engine.Tests;

[TestFixture]
public class DefaultContextLocNamingTests
{
    [TestCase( "", null, null, "", null )]
    [TestCase( "^", null, "", "", null )]
    [TestCase( "[]^", "", "", "", null )]
    [TestCase( "[H]loc^", "H", "loc", "", null )]
    [TestCase( "[H]loc^N", "H", "loc", "N", null )]
    [TestCase( "[H]loc^N(T)", "H", "loc", "N(T)", "T" )]
    [TestCase( "[H]loc^N(T(t))", "H", "loc", "N(T(t))", "T(t)" )]

    [TestCase( "CK.fTest", null, null, "CK.fTest", null )]
    [TestCase( "[]CK.fTest", "", null, "CK.fTest", null )]
    [TestCase( "[]^CK.fTest", "", "", "CK.fTest", null )]
    [TestCase( "^CK.fTest", null, "", "CK.fTest", null )]
    [TestCase( "[H]^CK.fTest", "H", "", "CK.fTest", null )]

    [TestCase( "[H]db^CK.fTest", "H", "db", "CK.fTest", null )]
    [TestCase( "[H]-db^CK.fTest", "H", "-db", "CK.fTest", null )]
    [TestCase( "[H]--db^CK.fTest", "H", "--db", "CK.fTest", null )]
    [TestCase( "[H]---srv-db^CK.fTest", "H", "---srv-db", "CK.fTest", null )]

    [TestCase( "srv-db^CK.fTest", null, "srv-db", "CK.fTest", null )]
    [TestCase( "[H]srvPrd-db^CK.fTest", "H", "srvPrd-db", "CK.fTest", null )]
    [TestCase( "[]srvPrd-db^CK.fTest", "", "srvPrd-db", "CK.fTest", null )]
    public void parsing_multiple_forms( string input, string expectedContext, string expectedLocation, string expectedName, string expectedTransformArg )
    {
        CheckParseWhole( input, expectedContext, expectedLocation, expectedName, expectedTransformArg );
        CheckParseSub( input, expectedContext, expectedLocation, expectedName, expectedTransformArg );
    }

    private static void CheckParseWhole(
        string input,
        string expectedContext,
        string expectedLocation,
        string expectedName,
        string expectedTransformArg )
    {
        string c, l, n, t;
        Assert.That( DefaultContextLocNaming.TryParse( input, out c, out l, out n, out t ) );
        CheckOutput( expectedContext, expectedLocation, expectedName, expectedTransformArg, c, l, n, t );
    }

    private static void CheckParseSub( string input, string expectedContext, string expectedLocation, string expectedName, string expectedTransformArg )
    {
        string c, l, n, t;
        Assert.That( DefaultContextLocNaming.TryParse( "?" + input, 1, out c, out l, out n, out t ) );
        CheckOutput( expectedContext, expectedLocation, expectedName, expectedTransformArg, c, l, n, t );
        Assert.That( DefaultContextLocNaming.TryParse( "??" + input, 2, out c, out l, out n, out t ) );
        CheckOutput( expectedContext, expectedLocation, expectedName, expectedTransformArg, c, l, n, t );
        Assert.That( DefaultContextLocNaming.TryParse( input + "pouf", 0, input.Length, out c, out l, out n, out t ) );
        CheckOutput( expectedContext, expectedLocation, expectedName, expectedTransformArg, c, l, n, t );
        Assert.That( DefaultContextLocNaming.TryParse( "pif" + input + "pouf", 3, input.Length, out c, out l, out n, out t ) );
        CheckOutput( expectedContext, expectedLocation, expectedName, expectedTransformArg, c, l, n, t );
    }

    private static void CheckOutput( string expectedContext, string expectedLocation, string expectedName, string expectedTransformArg, string c, string l, string n, string t )
    {
        Assert.That( c, Is.EqualTo( expectedContext ) );
        Assert.That( l, Is.EqualTo( expectedLocation ) );
        Assert.That( n, Is.EqualTo( expectedName ) );
        Assert.That( t, Is.EqualTo( expectedTransformArg ) );
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
        CheckCombineName( ".~", "A", expected: "", getNamespaceOnBaseNameGivesTheSame: false );
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
    public void combining_with_baseName_as_a_name_is_the_same_as_combining_to_the_namespace_of_the_baseName()
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

    [TestCase( "CK.fTest", null, null, "CK.fTest" )]
    [TestCase( "CK.fTest", "", null, "[]CK.fTest" )]
    [TestCase( "CK.fTest", "", "", "[]^CK.fTest" )]
    [TestCase( "CK.fTest", null, "", "^CK.fTest" )]
    [TestCase( "CK.fTest", "H", "db", "[H]db^CK.fTest" )]
    [TestCase( "^CK.fTest", "H", "nimp", "[H]^CK.fTest" )]
    [TestCase( "srv-db^CK.fTest", "H", "nimp", "[H]srv-db^CK.fTest" )]
    [TestCase( "[]srv-db^CK.fTest", "nimp", "nimp", "[]srv-db^CK.fTest" )]
    [TestCase( "[]^CK.fTest", "nimp", "nimp", "[]^CK.fTest" )]
    [TestCase( "^CK.fTest", "H", "nimp", "[H]^CK.fTest" )]
    [TestCase( "-^CK.fTest", null, "srv-db", "srv^CK.fTest" )]
    [TestCase( "--srv2-db3^CK.fTest", "", "sys-srv-db", "[]sys-srv2-db3^CK.fTest" )]
    [TestCase( "---srv2-db3^CK.fTest", "A", null, "[A]---srv2-db3^CK.fTest" )]
    public void resolving_context_and_location( string input, string curContext, string curLoc, string expectedResult )
    {
        Assert.That( DefaultContextLocNaming.Resolve( input, curContext, curLoc ), Is.EqualTo( expectedResult ) );
        Assert.That( DefaultContextLocNaming.Resolve( "?" + input, 1, curContext, curLoc ), Is.EqualTo( "?" + expectedResult ) );
        Assert.That( DefaultContextLocNaming.Resolve( "??" + input, 2, curContext, curLoc ), Is.EqualTo( "??" + expectedResult ) );
        Assert.That( DefaultContextLocNaming.Resolve( input + "pouf", 0, input.Length, curContext, curLoc ), Is.EqualTo( expectedResult + "pouf" ) );
        Assert.That( DefaultContextLocNaming.Resolve( "pif" + input + "pouf", 3, input.Length, curContext, curLoc ), Is.EqualTo( "pif" + expectedResult + "pouf" ) );

    }

    [TestCase( "-^H", "nimp", "" )]
    [TestCase( "--^H", "nimp", "oneLoc" )]
    public void ResolveException( string input, string curContext, string curLoc )
    {
        Assert.Throws<Exception>( () => DefaultContextLocNaming.Resolve( input, curContext, curLoc ) );
        Assert.Throws<Exception>( () => DefaultContextLocNaming.Resolve( "?" + input, 1, curContext, curLoc ) );
        Assert.Throws<Exception>( () => DefaultContextLocNaming.Resolve( "??" + input, 2, curContext, curLoc ) );
        Assert.Throws<Exception>( () => DefaultContextLocNaming.Resolve( input + "pouf", 0, input.Length, curContext, curLoc ) );
        Assert.Throws<Exception>( () => DefaultContextLocNaming.Resolve( "pif" + input + "pouf", 3, input.Length, curContext, curLoc ) );
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

    [TestCase( "x(y)", "y" )]
    [TestCase( "x(y(z))", "y(z)" )]
    [TestCase( "x(((A)))", "((A))" )]
    [TestCase( "x((B()(A)C)D)", "(B()(A)C)D" )]
    public void targetName_extraction( string s, string expected )
    {
        int len = s.Length;
        Assert.That( DefaultContextLocNaming.ExtractTransformArg( s, 0, ref len ), Is.EqualTo( expected ) );
    }
}
