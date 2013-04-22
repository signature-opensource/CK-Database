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
        public void SimpleSelect()
        {
            // We allow empty columns definition.
            Check( "select", "[select-()]" );
            Check( "select from a", "[select-()-from[a]]" );
            
            Check( "select 1", "[select-(1)]" );
            Check( "select 1, 2*5, *, (@i*7)", "[select-(1,[2*5],*,[@i*7])]" );
            Check( "select name, upper(N'u'+t.name) from sys.tables t", "[select-(name,call:upper([N'u'+t.name]))-from[¤{sys.tables-t}¤]]" );
            Check( "select name from sys.tables t inner join dbo.tC k on k.id = t.id or k.id=-t.id", "[select-(name)-from[¤{sys.tables-t-inner-join-dbo.tC-k-on-[[k.id=t.id]or[k.id=-[t.id]]]}¤]]" );

            Check( "select name n from a", "[select-(n-as-name)-from[a]]", textAutoCorrected: "select name as n from a" );
            Check( "select n=name from a", "[select-(n-=-name)-from[a]]" );
            Check( "select avg(z) [from] from a", "[select-([from]-as-call:avg(z))-from[a]]", textAutoCorrected: "select avg(z) as [from] from a" );
            Check( "select @X = avg(z) from a", "[select-(@X-=-call:avg(z))-from[a]]" );
        }

        [Test]
        public void SelectFromWhere()
        {
            Check( "select from dbo.fC( (5 * 2) ) where x = 4", "[select-()-from[call:dbo.fC([5*2])]-where[[x=4]]]" );
            Check( "select X from a where x = 4 order by z", "[select-(X)-from[a]-where[[x=4]]-orderBy[z]]" );
            Check( "select from t group by a, b with rollup", "[select-()-from[t]-groupBy[¤{a-,-b-with-rollup}¤]]" );
            Check( "select from t group by rollup(a,b)", "[select-()-from[t]-groupBy[call:rollup(a,b)]]" );
            
            Check( "select from t inner join z on z.id = [group].gid group by rollup(a,b)",
                    "[select-()-from[¤{t-inner-join-z-on-[z.id=[group].gid]}¤]-groupBy[call:rollup(a,b)]]" );

            Check( "select * from a order by z asc, r desc", "[select-(*)-from[a]-orderBy[¤{z-asc-,-r-desc}¤]]" );
            Check( @"SELECT top (3) DepartmentID, Name, GroupName
                        FROM HumanResources.Department
                        ORDER BY DepartmentID ASC 
                            OFFSET @StartingRowNumber - 1 ROWS 
                            FETCH NEXT @EndingRowNumber - @StartingRowNumber + 1 ROWS ONLY",
                    "[SELECT-top-(-3-)-(DepartmentID,Name,GroupName)-from[HumanResources.Department]-orderBy[¤{DepartmentID-ASC-OFFSET-[@StartingRowNumber-1]-ROWS-FETCH-NEXT-[[@EndingRowNumber-@StartingRowNumber]+1]-ROWS-ONLY}¤]]" );

            Check( "select from t group by rollup(a,b), nimp * [order by] order by s, k offset (@i+8) rows fetch next 45 - 8 rows only",
                    "[select-()-from[t]-groupBy[¤{call:rollup(a,b)-,-[nimp*[order by]]}¤]-orderBy[¤{s-,-k-call:offset([@i+8])-rows-fetch-next-[45-8]-rows-only}¤]]" );
        }

        [Test]
        public void ParseExpression01()
        {
            Check( "a", "a" );
            Check( "457", "457" );
            Check( " ( ( 457 ) ) ", "457" );
            Check( "(a)", "a" );
            Check( "*", "*" );
            Check( @"(""in"")", @"""in""" );
            Check( @"([is])", @"[is]" );

            Check( "a-b", "[a-b]" );
            Check( "(a-b)", "[a-b]" );
            Check( "( ( ( (a-b)   ))  )", "[a-b]" );

            Check( "(~2)", "~[2]" );
            Check( "~ 0 * 1 = (~2) * 3", "[[~[0]*1]=[~[2]*3]]" );
            Check( "0 + 1  * 2 >= ~3 / 4 + 1", "[[0+[1*2]]>=[[~[3]/4]+1]]" );
            Check( "1 = 1 and 0 = 0 and 2 = 2", "[[[1=1]and[0=0]]and[2=2]]" );
            Check( "1 = 1 and 0 = 0 or 2 = 2", "[[[1=1]and[0=0]]or[2=2]]" );
            Check( "1 = 1 or 1 = 1 and 0 = 1", "[[1=1]or[[1=1]and[0=1]]]" );
            Check( "(a+(b)+c)", "[[a+b]+c]" );
            Check( "(a >= b)", "[a>=b]" );
            Check( "(1 = 1 or 1 = 1) and 0 = 1", "[[[1=1]or[1=1]]and[0=1]]" );
            Check( "not 1 = 1 or 1 = 1", "[not[[1=1]]or[1=1]]" );
            Check( "not (1 = 1 or 1 = 1)", "not[[[1=1]or[1=1]]]" );
            Check( "a-b, a*8+3, (($78))", "{[a-b],[[a*8]+3],$78}" );
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
            Check( "(((4 + 5 * 8 not between 4 / 8 * 9 and 457))) = 4+7", "[NotBetween([4+[5*8]],[[4/8]*9],457)=[4+7]]" );
        }

        [Test]
        public void ParseIn()
        {
            Check( "@i in ( 1, 2, 3 )", "In(@i∈{1,2,3})" );
            Check( "@i not in ( 1, 2 )", "NotIn(@i∈{1,2})" );
            Check( "2*~5 not in ( 7 )", "NotIn([2*~[5]]∈{7})" );
            Check( "not 2*~5 not in ( 7 )", "not[NotIn([2*~[5]]∈{7})]" );
            Check( "not 2*~5 not in ( 7 ) or 1=1", "[not[NotIn([2*~[5]]∈{7})]or[1=1]]" );
            Check( "3 in (4+5,6,select Power from CK.tShmurtz) or 1=1", "[In(3∈{[4+5],6,[select-(Power)-from[CK.tShmurtz]]})or[1=1]]" );
            Check( "((((@i in ( 1, 2, 3 )))))", "In(@i∈{1,2,3})" );
        }

        [Test]
        public void ParseKoCall()
        {
            Check( "3 + AnyCall()", "[3+call:AnyCall()]" );
            Check( "3 + AnyCall(5, N'kjkj'+8)", "[3+call:AnyCall(5,[N'kjkj'+8])]" );
            Check( "3 < all (select Power from dbo.tNuclearPlant)", "[3<call:all([select-(Power)-from[dbo.tNuclearPlant]])]" );
            Check( "(3 < all (select Power from dbo.tNuclearPlant))", "[3<call:all([select-(Power)-from[dbo.tNuclearPlant]])]" );
            Check( "3 + ((AnyCall(5, N'kjkj'+8)))", "[3+call:AnyCall(5,[N'kjkj'+8])]" );
        }

        [Test]
        public void ParseIf()
        {
            var ifS = ParseStatement<SqlExprStIf>( @"if @i is null
                                                     print '1';
                                                   else print 2, 9, 'toto';" );

            Assert.That( ExplainWriter.Write( ifS ), Is.EqualTo( "if[IsNull(@i)]then[<print{'1'}>]else[<print{2,9,'toto'}>]" ) );

            ifS = ParseStatement<SqlExprStIf>( @"if exists(select t.* from sys.tables t) print N'OK';" );
            Assert.That( ExplainWriter.Write( ifS ), Is.EqualTo( "if[call:exists([select-(t.*)-from[¤{sys.tables-t}¤]])]then[<print{N'OK'}>]" ) );
        }

        private static void Check( string text, string explained, string textAutoCorrected = null )
        {
            SqlExpr e;
            var r = SqlAnalyser.ParseExpression( out e, text );
            Assert.That( r.IsError, Is.False, r.ToString() );
            Assert.That( ExplainWriter.Write( e ), Is.EqualTo( explained ) );
            Assert.That( textAutoCorrected ?? text, Is.EqualTo( e.ToString() ) );
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
