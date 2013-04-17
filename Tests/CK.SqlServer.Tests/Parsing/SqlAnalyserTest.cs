using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CK.SqlServer.Tests.Parsing
{
    [TestFixture]
    [Category( "SqlAnalyser" )]
    public class SqlAnalyserTest
    {
        [Test]
        public void ParseExpression01()
        {
            Check( "a", "a" );
            Check( "457", "457" );
            // Should support: Check( "((457))", "457" );

            // Identifiers are not enclosable: a generic block is built to carry the parenthesis.
            Check( "(a)", "¤{a}¤" );

            // SqlExprBinaryOperator is enclosable.
            Check( "a-b", "[a-b]" );
            Check( "(a-b)", "[a-b]" );
            Check( "( ( ( (a-b))))", "[a-b]" );
            // SqlExprUnaryOperator is enclosable.
            Check( "(~2)", "~[2]" );
            Check( "~ 0 * 1 = (~2) * 3", "[[~[0]*1]=[~[2]*3]]" );
            Check( "0 + 1  * 2 >= ~3 / 4 + 1", "[[0+[1*2]]>=[[~[3]/4]+1]]" );
            Check( "1 = 1 and 0 = 0 and 2 = 2", "[[[1=1]and[0=0]]and[2=2]]" );
            Check( "1 = 1 and 0 = 0 or 2 = 2", "[[[1=1]and[0=0]]or[2=2]]" );
            Check( "1 = 1 or 1 = 1 and 0 = 1", "[[1=1]or[[1=1]and[0=1]]]" );
            Check( "(a+(b)+c)", "[[a+¤{b}¤]+c]" );
            Check( "(a >= b)", "[a>=b]" );
            Check( "(1 = 1 or 1 = 1) and 0 = 1", "[[[1=1]or[1=1]]and[0=1]]" );
            Check( "not 1 = 1 or 1 = 1", "[not[[1=1]]or[1=1]]" );
            Check( "not (1 = 1 or 1 = 1)", "not[[[1=1]or[1=1]]]" );
            Check( "a-b, a*8+3", "{[a-b],[[a*8]+3]}" );
       }

        [Test]
        public void ParseIsNull()
        {
            Check( "~@i is null", "IsNull(~[@i])" );
            Check( "~@i is not null", "IsNotNull(~[@i])" );
            Check( "~@i * 8 is null", "IsNull([~[@i]*8])" );
            Check( "~@i * (a*b) is not null", "IsNotNull([~[@i]*[a*b]])" );
            Check( "not ~@i is null", "not[IsNull(~[@i])]" );
            Check( "not ~@i is null and 1=0", "[not[IsNull(~[@i])]and[1=0]]" );
            // IsNull is enclosable.
            Check( "not ((((~@i) is null)))", "not[IsNull(~[@i])]" );
            Check( "not ((((~@i) is not null)))", "not[IsNotNull(~[@i])]" );
        }

        [Test]
        public void ParseLike()
        {
            Check( "'text' like @i+@j and 1 = 1", "[Like('text',[@i+@j])and[1=1]]" );
            Check( "not 'text' like @i+@j and 1 = 1", "[not[Like('text',[@i+@j])]and[1=1]]" );
            Check( "'text' not like @i+@j or 1 = 1", "[NotLike('text',[@i+@j])or[1=1]]" );
            Check( "not 'text' not like @i+'p'+@j and 1 = 1", "[not[NotLike('text',[[@i+'p']+@j])]and[1=1]]" );

            Check( "'text' not like @i+@j escape N'e'", "NotLike('text',[@i+@j],N'e')" );
            Check( "'text' like @i+@j escape N'e' and 1 = 1", "[Like('text',[@i+@j],N'e')and[1=1]]" );

            // Like is enclosable.
            Check( "(('text' like @i+@j escape 'a')) and 1 = 1", "[Like('text',[@i+@j],'a')and[1=1]]" );
        }

        [Test]
        public void ParseBetween()
        {
            Check( "4 + 5 * 8 between 4 / 8 * 9 and 457 or 1=1", "[Between([4+[5*8]],[[4/8]*9],457)or[1=1]]" );
            Check( "4 + 5 * 8 between 4 / 8 * 9 and 457 and 1=1", "[Between([4+[5*8]],[[4/8]*9],457)and[1=1]]" );
            Check( "4 + 5 * 8 between 4 / 8 * 9 and 457 = 4+7", "[Between([4+[5*8]],[[4/8]*9],457)=[4+7]]" );
            Check( "4 + 5 * 8 not between 4 / 8 * 9 and 457 = 4+7", "[NotBetween([4+[5*8]],[[4/8]*9],457)=[4+7]]" );
            Check( "not 4 + 5 * 8 not between 4 / 8 * 9 and 457 or 1 = /*comment 4 Fun*/0", "[not[NotBetween([4+[5*8]],[[4/8]*9],457)]or[1=0]]" );
            // Between is enclosable.
            Check( "(((4 + 5 * 8 not between 4 / 8 * 9 and 457))) = 4+7", "[NotBetween([4+[5*8]],[[4/8]*9],457)=[4+7]]" );
        }

        [Test]
        public void ParseIn()
        {
            //Check( "@i in ( 1, 2, 3 )", "In(@i∈{1,2,3})" );
            //Check( "@i not in ( 1, 2 )", "NotIn(@i∈{1,2})" );
            //Check( "2*~5 not in ( 7 )", "NotIn([2*~[5]]∈{7})" );
            //Check( "not 2*~5 not in ( 7 )", "not[NotIn([2*~[5]]∈{7})]" );
            //Check( "not 2*~5 not in ( 7 ) or 1=1", "[not[NotIn([2*~[5]]∈{7})]or[1=1]]" );
            //Check( "3 in (4+5,6,select Power from CK.tShmurtz) or 1=1", "[In(3∈{[4+5],6,¤{select-Power-from-CK.tShmurtz}¤})or[1=1]]" );
            // In is enclosable.
            Check( "((((@i in ( 1, 2, 3 )))))", "In(@i∈{1,2,3})" );
        }

        [Test]
        public void ParseKoCall()
        {
            Check( "3 + AnyCall()", "[3+call:AnyCall()]" );
            Check( "3 + AnyCall(5, N'kjkj'+8)", "[3+call:AnyCall(5,[N'kjkj'+8])]" );
            Check( "3 < all (select Power from dbo.tNuclearPlant)", "[3<call:all(¤{select-Power-from-dbo.tNuclearPlant}¤)]" );
            Check( "(3 < all (select Power from dbo.tNuclearPlant))", "[3<call:all(¤{select-Power-from-dbo.tNuclearPlant}¤)]" );
            // KoCall is enclosable (except all, any and some).
            Check( "3 + ((AnyCall(5, N'kjkj'+8)))", "[3+call:AnyCall(5,[N'kjkj'+8])]" );
        }

        [Test]
        public void ParseIf()
        {
            var ifS = ParseStatement<SqlExprStIf>( @"if @i is null
                                                     print '1';
                                                   else print 2, 9, 'toto';" );

            Assert.That( ExplainWriter.Write( ifS ), Is.EqualTo( "if[IsNull(@i)]then[<print{'1'}>]else[<print{2,9,'toto'}>]" ) );

            ifS = ParseStatement<SqlExprStIf>( @"if exists(select * from sys.tables) print N'OK';" );
            Assert.That( ExplainWriter.Write( ifS ), Is.EqualTo( "if[call:exists(¤{select-*-from-sys.tables}¤)]then[<print{N'OK'}>]" ) );

        }

        private static void Check( string text, string explained )
        {
            SqlExpr e;
            var r = SqlAnalyser.ParseExpression( out e, text );
            Assert.That( r.IsError, Is.False, r.ToString() );
            Assert.That( ExplainWriter.Write( e ), Is.EqualTo( explained ) );
            Assert.That( text, Is.EqualTo( e.ToString() ) );
        }

        [Test]
        public void ParseStoredProcedure01()
        {
            var sp = ReadStatement<SqlExprStStoredProc>( "sStoredProcedure01.sql" );

            Assert.That( sp.Name.ToString(), Is.EqualTo( "CKCore.sErrorRethrow\r\n" ) );
            Assert.That( sp.Parameters[0].IsOutput, Is.False );
            Assert.That( sp.Parameters[0].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[0].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[0].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[0].Variable.Identifier.Name, Is.EqualTo( "@ProcId" ) );
            Assert.That( sp.Parameters[0].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.Int ) );
            Assert.That( sp.BodyStatements.Statements.Count, Is.EqualTo( 2 ) );
        }

        [DebuggerStepThrough]
        private static T ReadStatement<T>( string fileName ) where T : SqlExprBaseSt
        {
            return ParseStatement<T>( TestHelper.LoadTextFromParsingScripts( fileName ) );
        }

        [DebuggerStepThrough]
        private static T ParseStatement<T>( string text ) where T : SqlExprBaseSt
        {
            SqlExprBaseSt statement;
            SqlAnalyser.ErrorResult r = SqlAnalyser.ParseStatement( out statement, text );
            Assert.That( !r.IsError, r.ToString() );
            Assert.That( statement, Is.InstanceOf<T>() );
            T s = (T)statement;
            return s;
        }

    }
}
