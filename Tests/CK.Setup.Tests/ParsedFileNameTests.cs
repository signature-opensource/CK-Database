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
            Assert.That( ParsedFileName.TryParse( "Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.ExtraPath, Is.Null );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.IsContent, Is.False );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.None ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "Test.InstallContent.1.2.3.to.1.2.4.sql", @"C:\", true, out result ) );
            Assert.That( result.ExtraPath, Is.EqualTo( @"C:\" ) );
            Assert.That( result.FromVersion, Is.EqualTo( new Version( 1, 2, 3 ) ) );
            Assert.That( result.Version, Is.EqualTo( new Version( 1, 2, 4 ) ) );
            Assert.That( result.IsContent, Is.True );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Install ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "Test.Settle", "CK.Test", false, out result ) );
            Assert.That( result.ExtraPath, Is.EqualTo( "CK.Test" ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.IsContent, Is.False );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Settle ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "Test.SettleContent.sql", null, true, out result ) );
            Assert.That( result.FromVersion, Is.Null );
            Assert.That( result.Version, Is.Null );
            Assert.That( result.IsContent, Is.True );
            Assert.That( result.SetupStep, Is.EqualTo( SetupStep.Settle ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );

        }

        [Test]
        public void FileNameParserWithContext()
        {
            ParsedFileName result;
            Assert.That( ParsedFileName.TryParse( "[db]Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.FullName, Is.EqualTo( "[db]Test" ) );
            Assert.That( result.FullNameWithoutContext, Is.EqualTo( "Test" ) );

            // Dot after ] is ignored.
            Assert.That( ParsedFileName.TryParse( "[db].Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.FullName, Is.EqualTo( "[db]Test" ) );
            Assert.That( result.FullNameWithoutContext, Is.EqualTo( "Test" ) );

            // Empty context is ignored.
            Assert.That( ParsedFileName.TryParse( "[].Test.1.2.3.sql", null, true, out result ) );
            Assert.That( result.FullName, Is.EqualTo( "Test" ) );
            Assert.That( result.FullNameWithoutContext, Is.EqualTo( "Test" ) );

            Assert.That( ParsedFileName.TryParse( "[].A", null, false, out result ), Is.True );
            Assert.That( result.FullName, Is.EqualTo( "A" ) );
            Assert.That( result.FullNameWithoutContext, Is.EqualTo( "A" ) );
            
            Assert.That( ParsedFileName.TryParse( "[]A", null, false, out result ), Is.True );
            Assert.That( result.FullName, Is.EqualTo( "A" ) );
            Assert.That( result.FullNameWithoutContext, Is.EqualTo( "A" ) );
            
            // Invalid context is not parsed.
            Assert.That( ParsedFileName.TryParse( "[db.Test.1.2.3.sql", null, true, out result ), Is.False );
            Assert.That( ParsedFileName.TryParse( "[", null, false, out result ), Is.False );
            Assert.That( ParsedFileName.TryParse( "[]", null, false, out result ), Is.False );
            Assert.That( ParsedFileName.TryParse( "[].", null, false, out result ), Is.False );

        }
    }
}
