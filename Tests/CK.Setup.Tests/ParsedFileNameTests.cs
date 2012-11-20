using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Xml;
using System.Xml.Linq;

namespace CK.Setup.Tests
{
    [TestFixture]
    public class ParsedFileNameTests
    {
        [Test]
        public void FileNameParser()
        {
            ParsedFileName result;
            Assert.That( ParsedFileName.TryParse( "C", "Loc", "Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.ExtraPath, Is.Null );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.IsContent, Is.False );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.None ) );
            Assert.That( result.FullName, Is.EqualTo( "[C]Loc^Test" ) );

            Assert.That( ParsedFileName.TryParse( String.Empty, "Loc", "Test.InstallContent.1.2.3.to.1.2.4.sql", @"C:\", true, out result ) );
            Assert.That( result.ExtraPath, Is.EqualTo( @"C:\" ) );
            Assert.That( result.FromVersion, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 4 ) ) );
            Assert.That( result.IsContent, Is.True );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Install ) );
            Assert.That( result.FullName, Is.EqualTo( "[]Loc^Test" ) );

            Assert.That( ParsedFileName.TryParse( "C", null, "Test.Settle", "CK.Test", false, out result ) );
            Assert.That( result.ExtraPath, Is.EqualTo( "CK.Test" ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.IsContent, Is.False );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Settle ) );
            Assert.That( result.FullName, Is.EqualTo( "[C]Test" ) );

            Assert.That( ParsedFileName.TryParse( null, "Loc", "Test.SettleContent.sql", null, true, out result ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.IsContent, Is.True );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Settle ) );
            Assert.That( result.FullName, Is.EqualTo( "Loc^Test" ) );

        }

        [Test]
        public void FileNameParserWithContext()
        {
            ParsedFileName result;
            Assert.That( ParsedFileName.TryParse( "nimp", null, "[db]Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.FullName, Is.EqualTo( "[db]Test" ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

            // Empty context is the Default.
            Assert.That( ParsedFileName.TryParse( "nimp", "db", "[]Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.FullName, Is.EqualTo( "db^Test" ) );
            Assert.That( result.Context, Is.Null );
            Assert.That( result.Location, Is.EqualTo( "db" ) );
            Assert.That( result.Name, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "nimp", "nimp", "[]db^A.1.0.3", null, false, out result ), Is.True );
            Assert.That( result.FullName, Is.EqualTo( "[]db^A" ) );
            Assert.That( result.Context, Is.EqualTo( "" ) );
            Assert.That( result.Location, Is.EqualTo( "db" ) );
            Assert.That( result.Name, Is.EqualTo( "A" ) );
            Assert.That( result.Version.ToString(), Is.EqualTo( "1.0.3" ) );

            Assert.That( ParsedFileName.TryParse( "nimp", null, "[]", null, false, out result ), Is.False );
            Assert.That( result.FullName, Is.EqualTo( "[]" ) );
            Assert.That( result.Context, Is.EqualTo( "" ) );
            Assert.That( result.Location, Is.Null );
            Assert.That( result.Name, Is.EqualTo( "" ) );
            Assert.That( result.Version, Is.Null );
            
            // Invalid context is not parsed.
            Assert.That( ParsedFileName.TryParse( "nimp", "nimp", "[db.Test.1.2.3.sql", null, true, out result ), Is.False );
            Assert.That( ParsedFileName.TryParse( "nimp", "nimp", "[", null, false, out result ), Is.False );

        }
    }
}
