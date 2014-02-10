using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using NUnit.Framework;

namespace CK.SqlServer.Setup.Engine.Tests
{

    [TestFixture]
    public class ShallowParserTests
    {
        [Test]
        public void HeaderParsing()
        {
            // Buggy Header (ending with ')' instead of '}'). 
            string h = @"--Version = *, Requires= { CV.vFolderBase, CV.vAnswer )
create view CV.vQuestionAnsweredCount
as
    select * from CV.vAnswer;
";
            SqlObjectProtoItem item = SqlObjectParser.Create( TestHelper.ConsoleMonitor, new ContextLocName( "CV.vQuestionAnsweredCount" ), h );
            Assert.That( item.ItemKind, Is.EqualTo( CK.Setup.DependentItemKind.Item ) );
            Assert.That( item.ItemType, Is.EqualTo( SqlObjectProtoItem.TypeView ) );
            Assert.That( item.Version, Is.Null );
        }

    }
}
