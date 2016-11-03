#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setupable.Engine.Tests\ParsedFileNameTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
        public void ParsedFileName_Can_be_created_from_source()
        {
            var locName = new ContextLocName( "[]db^Name" );
            ParsedFileName result = ParsedFileName.CreateFromSource( locName, "sql" );
            Assert.That( result.FullName, Is.EqualTo( "[]db^Name" ) );
            Assert.That( result.FileName, Is.EqualTo( "ParsedFileNameTests.cs@64" ) );
            Assert.That( result.Extension, Is.EqualTo( "sql" ) );
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
