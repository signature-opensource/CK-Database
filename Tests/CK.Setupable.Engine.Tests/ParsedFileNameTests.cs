using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Xml;
using System.Xml.Linq;
using CK.Setup;

namespace CK.Setupable.Engine.Tests
{
    [TestFixture]
    public class ParsedFileNameTests
    {
        [Test]
        public void ParsedFileName_captures_all_the_step_version_and_extension()
        {
            ParsedFileName result;
            Assert.That( ParsedFileName.TryParse( "C", "Loc", "Test.1.2.3.sql", null, out result ) );
            Assert.That( result.ExtraPath, Is.Null );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.SetupStep, Is.EqualTo( SetupCallGroupStep.None ) );
            Assert.That( result.FullName, Is.EqualTo( "[C]Loc^Test" ) );

            Assert.That( ParsedFileName.TryParse( string.Empty, "Loc", "Test.InstallContent.1.2.3.to.1.2.4.sql", @"C:\", out result ) );
            Assert.That( result.ExtraPath, Is.EqualTo( @"C:\" ) );
            Assert.That( result.FromVersion, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 4 ) ) );
            Assert.That( result.SetupStep, Is.EqualTo( SetupCallGroupStep.InstallContent ) );
            Assert.That( result.FullName, Is.EqualTo( "[]Loc^Test" ) );

            Assert.That( ParsedFileName.TryParse( "C", null, "Test.Settle", "CK.Test", out result, "extension" ) );
            Assert.That( result.ExtraPath, Is.EqualTo( "CK.Test" ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Extension, Is.EqualTo( "extension" ) );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.SetupStep, Is.EqualTo( SetupCallGroupStep.Settle ) );
            Assert.That( result.FullName, Is.EqualTo( "[C]Test" ) );

            Assert.That( ParsedFileName.TryParse( null, "Loc", "Test.SettleContent.sql", null, out result ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
            Assert.That( result.SetupStep, Is.EqualTo( SetupCallGroupStep.SettleContent ) );
            Assert.That( result.FullName, Is.EqualTo( "Loc^Test" ) );
        }

        [Test]
        public void ParsedFileName_can_be_created_from_source()
        {
            var locName = new ContextLocName( "[]db^Name" );
            ParsedFileName result = ParsedFileName.CreateFromSourceCode( locName, "sql" );
            Assert.That( result.FullName, Is.EqualTo( "[]db^Name" ) );
            Assert.That( result.FileName, Is.EqualTo( "ParsedFileNameTests.cs@63.sql" ) );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
        }

        [Test]
        public void ParsedFileName_when_created_from_source_with_step_and_versions_exposes_them_in_its_FileName()
        {
            var locName = new ContextLocName( "[]db^Name" );
            ParsedFileName result = ParsedFileName.CreateFromSourceCode( locName, "sql", SetupCallGroupStep.Init, new Version( 1, 0, 0 ), new Version( 2, 0, 0 ), "HardCodedFile", 3712 );
            Assert.That( result.FullName, Is.EqualTo( "[]db^Name" ) );
            Assert.That( result.FileName, Is.EqualTo( "HardCodedFile@3712.sql-Init.1.0.0.to.2.0.0" ) );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
        }

        [TestCase( "[]db^Name(T(a(b))).sql", "T(a(b))", null )]
        [TestCase( "[]db^Name(T(a)).1.0.0.sql", "T(a)", "1.0.0" )]
        public void ParsedFileName_works_on_strange_names( string fName, string transformArg, string version )
        {
            ParsedFileName result;
            Assert.That( ParsedFileName.TryParse( null, null, fName, null, out result ) );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
            Assert.That( result.Version, Is.EqualTo( version != null ? Version.Parse( version ) : null ) );
            Assert.That( result.TransformArg, Is.EqualTo( transformArg ) );
        }


        [Test]
        public void ParsedFileName_parsing_handles_context_and_location_from_context()
        {
            ParsedFileName result;
            Assert.That( ParsedFileName.TryParse( "nimp", null, "[db]Test.1.2.3.sql", null, out result ) );
            Assert.That( result.FullName, Is.EqualTo( "[db]Test" ) );

            // Empty context is the Default.
            Assert.That( ParsedFileName.TryParse( "nimp", "db", "[]Test.1.2.3.sql", null, out result ) );
            Assert.That( result.FullName, Is.EqualTo( "[]db^Test" ) );
            Assert.That( result.Context, Is.Empty );
            Assert.That( result.Location, Is.EqualTo( "db" ) );
            Assert.That( result.Name, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "nimp", "nimp", "[]db^A.1.0.3", null, out result, "sql" ), Is.True );
            Assert.That( result.FullName, Is.EqualTo( "[]db^A" ) );
            Assert.That( result.Context, Is.EqualTo( "" ) );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
            Assert.That( result.Location, Is.EqualTo( "db" ) );
            Assert.That( result.Name, Is.EqualTo( "A" ) );
            Assert.That( result.Version.ToString(), Is.EqualTo( "1.0.3" ) );

            Assert.That( ParsedFileName.TryParse( "nimp", null, "[]", null, out result, "sql" ), Is.False );
            Assert.That( result, Is.Null );

            Assert.That( ParsedFileName.TryParse( "nimp", null, "nameWithoutExtension", null, out result ), Is.False );
            Assert.That( result, Is.Null );

            // Invalid context is not parsed.
            Assert.That( ParsedFileName.TryParse( "nimp", "nimp", "[db.Test.1.2.3.sql", null, out result ), Is.False );
            Assert.That( ParsedFileName.TryParse( "nimp", "nimp", "[", null, out result, "ext" ), Is.False );

        }
    }
}
