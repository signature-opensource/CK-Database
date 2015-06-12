using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CK.SqlServer.Parser.Tests
{
    [TestFixture]
    public class SetStatementTests
    {

        [TestCase( "set @v = 5" )]
        [TestCase( "set @v = 5;" )]
        [TestCase( "set @v = 5; select 1" )]
        [TestCase( "set @v = 5 declare @i" )]
        public void setting_a_simple_variable( string text )
        {
            var e = TestHelper.ParseOneStatement<SqlExprStSetVar>( text );
            Assert.That( e.Variable.Name, Is.EqualTo( "@v" ) );
            Assert.That( e.Value, Is.InstanceOf<SqlExprLiteral>() );
            Assert.That( ((SqlExprLiteral)e.Value).Token.LiteralValue, Is.EqualTo( "5" ) );
        }

        [TestCase( "set transaction isolation level" )]
        [TestCase( "set transaction isolation level;" )]
        [TestCase( "set transaction isolation level; select 1;" )]
        [TestCase( "set transaction isolation level select 1;" )]
        public void setting_an_option_is_an_unmodelled( string text )
        {
            var e = TestHelper.ParseOneStatement<SqlExprStSetOpt>( text );
            Assert.That( e.SetT.Name, Is.EqualTo( "set" ) );
            CollectionAssert.AreEqual( new[]{ "transaction", "isolation", "level" }, e.List.TokensWithoutParenthesis.Select( t => t.ToString() ) );
        }

        [Test]
        public void setting_with_syntax_error()
        {
            SqlExprBaseSt e;
            SqlAnalyser.ErrorResult r = SqlAnalyser.ParseStatement( out e, "set @v = (select 1==0);" );
            Assert.That( r.IsError );
            Assert.That( r.ErrorMessage, Is.StringStarting( "Expected expression" ).And.StringContaining( "<- Unexpected '='" ) );
        }
    }
}
