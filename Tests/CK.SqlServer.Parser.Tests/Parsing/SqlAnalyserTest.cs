#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Parser.Tests\Parsing\SqlAnalyserTest.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CK.SqlServer.Parser.Tests
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
            Check( "select X from a where x = 4 order by z", "OrderBy([select-(X)-from[a]-where[[x=4]]],(z))" );
            Check( "select from t group by a, b with rollup", "[select-()-from[t]-groupBy[¤{a-,-b-with-rollup}¤]]" );
            Check( "select from t group by rollup(a,b)", "[select-()-from[t]-groupBy[call:rollup(a,b)]]" );

            Check( "select from t inner join z on z.id = [group].gid group by rollup(a,b)",
                    "[select-()-from[¤{t-inner-join-z-on-[z.id=[group].gid]}¤]-groupBy[call:rollup(a,b)]]" );

            Check( "select * from a order by z asc, r desc", "OrderBy([select-(*)-from[a]],(z-asc,r-desc))" );
            Check( @"SELECT top (3) DepartmentID, Name, GroupName
                        FROM HumanResources.Department
                        order by DepartmentID ASC, Shmurtz 
                            OFFSET @StartingRowNumber - 1 ROWS 
                            FETCH NEXT @EndingRowNumber - @StartingRowNumber + 1 ROWS ONLY",
                    @"OrderBy( [SELECT-top-(-3-)-(DepartmentID,Name,GroupName)-from[HumanResources.Department]],
                               (DepartmentID-ASC, Shmurtz), offset: [@StartingRowNumber-1], fetch: [[@EndingRowNumber-@StartingRowNumber]+1]
                             )" );

            Check( "select from t group by rollup(a,b), nimp * [order\"by] order by s, k offset (@i+8) rows fetch next 45 - 8 rows only",
                    @"OrderBy( 
                                [select-()-from[t]-groupBy[¤{call:rollup(a,b)-,-[nimp*[order""by]]}¤]],
                                (s,k),
                                offset: [@i+8], 
                                fetch: [45-8]
                              )" );
        }
        
        [Test]
        public void WindowingFunctions()
        {
            string s;

            s = @"SELECT ROW_NUMBER() OVER(PARTITION BY PostalCode ORDER BY SalesYTD DESC) AS RowNumber from table";
            Check( s, "[SELECT-(RowNumber-AS-call:ROW_NUMBER()OVER[¤{PARTITION-BY-PostalCode-ORDER-BY-SalesYTD-DESC}¤])-from[table]]" );

            s = @"SELECT SalesOrderID, ProductID, OrderQty,
                        SUM(OrderQty) OVER(PARTITION BY SalesOrderID) AS Total,
                        ""PercentByProductID"" = CAST(1. * OrderQty / SUM(OrderQty) OVER(PARTITION BY SalesOrderID)*100 AS DECIMAL(5,2))
                  FROM Sales.SalesOrderDetail 
                  WHERE SalesOrderID IN(43659,43664)";
            Check( s, @"[SELECT-( 
                                    SalesOrderID, 
                                    ProductID, 
                                    OrderQty,
                                    Total-AS-call:SUM(OrderQty)OVER[¤{PARTITION-BY-SalesOrderID}¤],
                                    ""PercentByProductID""-=-(DECIMAL-(-5-,-2-))[[[[1.*OrderQty]/call:SUM(OrderQty)OVER[¤{PARTITION-BY-SalesOrderID}¤]]*100]]
                                )
                               -from[Sales.SalesOrderDetail]
                               -where[In(SalesOrderID∈{43659,43664})]
                        ]" );

        }

        [Test]
        public void CollationTests()
        {
            string s = @"
     Select id as ID,
            GreekCol COLLATE latin1_general_ci_as as T11,
            LatinCol COLLATE greek_ci_as as [collate]
            from TestTab11
    UNION
	 Select id as ID,
			T11 = GreekCol COLLATE latin1_general_ci_as,
			LatinCol
			from TestTab12
	UNION
     Select id as ID,
            GreekCol COLLATE latin1_general_ci_as + LatinCol,
            LatinCol + GreekCol COLLATE greek_ci_as
            from TestTab12
	UNION
     Select id as ID,
            GreekCol + LatinCol COLLATE latin1_general_ci_as,
            LatinCol COLLATE greek_ci_as + GreekCol COLLATE greek_ci_as
            from TestTab12
	UNION
     Select id as ID,
            GreekCol + LatinCol COLLATE latin1_general_ci_as,
            ( (((('U' COLLATE greek_ci_as)))) + (((( ((LatinCol)) COLLATE greek_ci_as)))) ) + GreekCol  + 'P'
            from TestTab12";

            Check( s, @"[
                            [
                                [
                                    [
                                        [Select-(
                                                  ID-as-id,
                                                  T11-as-GreekCol-COLLATE-latin1_general_ci_as,
                                                  [collate]-as-LatinCol-COLLATE-greek_ci_as
                                                )
                                               -from[TestTab11]
                                        ]
                                        UNION
                                        [Select-(
                                                    ID-as-id,
                                                    T11-=-GreekCol-COLLATE-latin1_general_ci_as,
                                                    LatinCol
                                                )
                                               -from[TestTab12]
                                        ]
                                    ]
                                    UNION
                                    [Select-(
                                                ID-as-id,
                                                [GreekCol-COLLATE-latin1_general_ci_as+LatinCol],
                                                [LatinCol+GreekCol-COLLATE-greek_ci_as]
                                            )
                                           -from[TestTab12]
                                    ]
                                ]
                                UNION
                                [Select-(
                                            ID-as-id,
                                            [GreekCol+LatinCol-COLLATE-latin1_general_ci_as],
                                            [LatinCol-COLLATE-greek_ci_as+GreekCol-COLLATE-greek_ci_as]
                                        )
                                       -from[TestTab12]
                                ]
                            ]
                            UNION
                            [Select-(
                                        ID-as-id,
                                        [GreekCol+LatinCol-COLLATE-latin1_general_ci_as],
                                        [[[(-(-(-(-'U'-COLLATE-greek_ci_as-)-)-)-)+(-(-(-(-(-(-LatinCol-)-)-COLLATE-greek_ci_as-)-)-)-)]+GreekCol]+'P']
                                    )
                                   -from[TestTab12]
                            ]
                        ]" );

        }

        [Test]
        public void SelectUnionIntersect()
        {
            // -- intersect > union
            var intersectStrongerThanUnion = @"[
                                                    [select-(1)]
                                                  union
                                                    [ [select-(2)]intersect[select-(0)] ]
                                               ]";
            Check( "select 1 union select 2 intersect select 0", intersectStrongerThanUnion );
	        // -- Same as 
	        Check( "select 1 union (select 2 intersect select 0)", intersectStrongerThanUnion );
	        // -- Not the same as 
            Check( "(select 1 union select 2) intersect select 0", @"[
                                                                          [ [select-(1)]union[select-(2)] ]
                                                                       intersect
                                                                          [select-(0)]
                                                                     ]" );
        }

        [Test]
        public void SelectUnionAllExcept()
        {
            // -- except > union
            var exceptStrongerThanUnion = @"[
                                                [select-(1)]
                                              union-all
                                                [ [select-(2)]except[select-(1)] ]
                                            ]";
            Check( "select 1 union all select 2 except select 1", exceptStrongerThanUnion );

	        // -- Same as 
	        Check( "select 1 union all (select 2 except select 1)", exceptStrongerThanUnion );
	        
            // -- Not the same as 
            Check( "(select 1 union all select 2) except select 1", @"[
                                                                            [ [select-(1)]union-all[select-(2)] ]
                                                                        except
                                                                            [select-(1)]
                                                                      ]" );
        }

        [Test]
        public void SelectExceptIntersect()
        {
            // -- intersect > except
            var sc1 = "(select 1 union select 2 union select 3)";
            var c1 = "[[[select-(1)]union[select-(2)]]union[select-(3)]]";
            Check( sc1, c1 );
            
            var sc2 = "(select 1 union select 2)";
            var c2 = "[[select-(1)]union[select-(2)]]";
            Check( sc2, c2 );
            
            var sc3 = "(select 1)";
            var c3 = "[select-(1)]";
            Check( sc3, c3 );

            var intersectStrongerThanExpect = "["+c1+"except["+c2+"intersect"+c3+"]]";
            Check( sc1 +" except "+ sc2 + " intersect " + sc3, intersectStrongerThanExpect );
	        // -- Same as 
            Check( sc1 + " except " + "(" + sc2 + " intersect " + sc3 + ")", intersectStrongerThanExpect );
            // -- Not the same as 
            Check( "(" + sc1 + " except " + sc2 + ")" + " intersect " + sc3, "[[" + c1 + "except" + c2 + "]intersect" + c3 + "]" );
        }

        [Test]
        public void SelectUnionAndOrderBy()
        {
            {
                var sc1 = "(((((select name from sys.tables where X))))) order by name";
                Check( sc1, "OrderBy([select-(name)-from[sys.tables]-where[X]],(name))" );
            }
            {
                // This is not syntaxically valid.
                var sc1 = "((select name from sys.tables where X) order by name) for xml auto";
                Check( sc1, "For(OrderBy([select-(name)-from[sys.tables]-where[X]],(name)),¤{xml-auto}¤)" );
            }
            {
                var sc1 = @"((((
	                            (select name from sys.tables where name like '%a%')
                            union
	                            (((select 'u'+name from sys.tables where name like '%a%')))
                            ))))
                            order by name desc
                            for xml auto";
                var sc2 = @"select name from sys.tables where name like '%a%'
                            union
                            select 'u'+name from sys.tables where name like '%a%'
                            order by name desc
                            for xml auto";
                var c = @"For(
                                OrderBy(
                                        [
                                                [select-(name)-from[sys.tables]-where[Like(name,'%a%')]]
                                             union
                                                [select-(['u'+name])-from[sys.tables]-where[Like(name,'%a%')]]
                                        ], (name-desc)
                                     ), ¤{xml-auto}¤
                              )";

                Check( sc1, c );
                Check( sc2, c );
            }
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
        public void ParseSimpleCase()
        {
            Check( "case @i when 0 then 1 end", "case(@i):0=>1" );
            Check( "case @i+7 when 0.2 then 1.6 when 3+@i then null end", "case([@i+7]):0.2=>1.6:[3+@i]=>null" );
            Check( "case 0 when 0 then 1 else 2 end", "case(0):0=>1:2" );
            Check( "case 0 when 0 then 1 else @i*8 end", "case(0):0=>1:[@i*8]" );
        }

        [Test]
        public void ParseSearchCase()
        {
            Check( "case when 0=0 then 1 end", "case:[0=0]=>1" );
            Check( "case when 0>1 then 1 when 0<1 then null end", "case:[0>1]=>1:[0<1]=>null" );
            Check( "case when 0<>5 then 4 else ((5/8)) end", "case:[0<>5]=>4:[5/8]" );
        }

        [Test]
        public void ParseSelectAssign()
        {
            Check( "select @hid = hierarchyid::GetRoot(), @i = 87/7", "[select-(@hid-=-call:hierarchyid::GetRoot(),@i-=-[87/7])]" );
        }

        [Test]
        public void ParseIf()
        {
            var ifS = ParseStatement<SqlExprStIf>( @"if @i is null
                                                     print '1';
                                                   else print 2, 9, 'toto';" );

            Assert.That( ExplainWriter.Write( ifS ), Is.EqualTo( "if[IsNull(@i)]then[<¤{print-'1'}¤>]else[<¤{print-2-,-9-,-'toto'}¤>]" ) );

            ifS = ParseStatement<SqlExprStIf>( @"if exists(select t.* from sys.tables t) print N'OK';" );
            Assert.That( ExplainWriter.Write( ifS ), Is.EqualTo( "if[call:exists([select-(t.*)-from[¤{sys.tables-t}¤]])]then[<¤{print-N'OK'}¤>]" ) );
        }

        private static void Check( string text, string explained, string textAutoCorrected = null )
        {
            SqlExpr e;
            var r = SqlAnalyser.ParseExpression( out e, text );
            Assert.That( r.IsError, Is.False, r.ToString() );
            Assert.That( ExplainWriter.Write( e ), Is.EqualTo( Regex.Replace( explained, @"\s*", String.Empty ) ) );
            Assert.That( e.ToString(), Is.EqualTo( textAutoCorrected ?? text ) );
        }

        [Test]
        public void ParseStoredProcedureWithoutTerminator()
        {
            var sp = ReadStatement<SqlExprStStoredProc>( "sProcWithoutTerminator.sql" );

            Assert.That( sp.Name.ToString(), Is.EqualTo( "sProcWithoutTerminator\r\n" ) );
            Assert.That( sp.Parameters[0].IsOutput, Is.False );
            Assert.That( sp.Parameters[0].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[0].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[0].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[0].Variable.Identifier.Name, Is.EqualTo( "@P" ) );
            Assert.That( sp.Parameters[0].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.Int ) );
            Assert.That( sp.BodyStatements.Statements.Count, Is.EqualTo( 1 ) );
        }

        [Test]
        public void ParseFunctionAclGrantLevel()
        {
            var f = ReadStatement<SqlExprStFunctionScalar>( "fAclGrantLevel.sql" );

            Assert.That( f.Name.ToString(), Is.EqualTo( "CK.fAclGrantLevel\r\n" ) );
            Assert.That( f.Parameters[0].IsOutput, Is.False );
            Assert.That( f.Parameters[0].IsReadOnly, Is.False );
            Assert.That( f.Parameters[0].DefaultValue, Is.Null );
            Assert.That( f.Parameters[0].Variable.Identifier.IsVariable, Is.True );
            Assert.That( f.Parameters[0].Variable.Identifier.Name, Is.EqualTo( "@ActorId" ) );
            Assert.That( f.Parameters[0].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.Int ) );
            Assert.That( f.Parameters.Count, Is.EqualTo( 2 ) );
            Assert.That( f.Parameters[1].Variable.Identifier.Name, Is.EqualTo( "@AclId" ) );
            Assert.That( f.Parameters[1].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.Int ) );
            Assert.That( f.ReturnsT, Is.Not.Null );
            Assert.That( f.ReturnedType.ActualType.DbType, Is.EqualTo( SqlDbType.TinyInt ) );
            Assert.That( f.BodyStatements.Statements.Count, Is.EqualTo( 1 ) );
            Assert.That( f.BodyStatements.Statements[0], Is.InstanceOf<SqlExprStReturn>() );
            SqlExprStReturn r = (SqlExprStReturn)f.BodyStatements.Statements[0];
            SqlExprKoCall isNull = (SqlExprKoCall)r.Value;
            SqlExprKoCall isNull2 = (SqlExprKoCall)isNull.Parameters[1];
            SqlExprLiteral zero = (SqlExprLiteral)isNull2.Parameters[1];

            Assert.That( zero.Token.LiteralValue, Is.EqualTo( "0" ) );
        }


        [Test]
        public void ParseStoredProcedureWithOptions()
        {
            var sp = ReadStatement<SqlExprStStoredProc>( "sWithOptions.sql" );

            Assert.That( sp.Name.ToString(), Is.EqualTo( "sWithOptions\r\n" ) );
            Assert.That( sp.Parameters.Count, Is.EqualTo( 0 ) );
            Assert.That( sp.Options.Items.Count(), Is.EqualTo( 4 ), "[with] [recompile] [,] [execute as owner]" );
            Assert.That( sp.Options.Tokens.ToStringWithoutTrivias( "|" ), Is.EqualTo( "with|recompile|,|execute|as|owner" ) );

            Assert.That( sp.BodyStatements.Statements.Count, Is.EqualTo( 2 ) );
        }

        [Test]
        public void ParseStrangeStoredProcedure()
        {
            var sp = ReadStatement<SqlExprStStoredProc>( "sStrange.sql" );

            Assert.That( sp.Name.ToString(), Is.EqualTo( "sStrange\r\n" ) );
            Assert.That( sp.Parameters, Is.Empty );
            Assert.That( sp.BodyStatements.Statements.Count, Is.EqualTo( 5 ) );
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

        [Test]
        public void ParseStoredProcedure02()
        {
            var sp = ReadStatement<SqlExprStStoredProc>( "sStoredProcedure02.sql" );

            Assert.That( sp.Name.ToString(), Is.EqualTo( "CK.sResDataStringSet -- Merge inside.\r\n" ) );
            Assert.That( sp.Parameters.Count, Is.EqualTo( 2 ) );
            Assert.That( sp.BodyStatements.Statements.Count, Is.EqualTo( 1 ), "Unmodeled." );
        }

        [Test]
        public void ParseStoredProcedure03()
        {
            var sp = ReadStatement<SqlExprStStoredProc>( "sStoredProcedure03.sql" );

            Assert.That( sp.Name.ToString(), Is.EqualTo( "InvBack.sOfferCreate\r\n" ) );
            Assert.That( sp.Parameters.Count, Is.EqualTo( 7 ) );
        }

        [Test]
        public void ParseStoredProcedureInputOutput()
        {
            var sp = ReadStatement<SqlExprStStoredProc>( "sStoredProcedureInputOutput.sql" );

            Assert.That( sp.Name.IdentifierAt(1).Name, Is.EqualTo( "sStoredProcedureInputOutput" ) );

            Assert.That( sp.Parameters[0].IsOutput, Is.False );
            Assert.That( sp.Parameters[0].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[0].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[0].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[0].Variable.Identifier.Name, Is.EqualTo( "@p1" ) );
            Assert.That( sp.Parameters[0].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.Int ) );
            Assert.That( sp.Parameters[0].Variable.TypeDecl.ActualType.SyntaxSize, Is.EqualTo( -2 ), "Size does not apply." );

            Assert.That( sp.Parameters[1].IsOutput, Is.False );
            Assert.That( sp.Parameters[1].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[1].DefaultValue, Is.Not.Null );
            Assert.That( sp.Parameters[1].DefaultValue.ToString(), Is.EqualTo( "= 0" ) );
            Assert.That( sp.Parameters[1].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[1].Variable.Identifier.Name, Is.EqualTo( "@p2" ) );
            Assert.That( sp.Parameters[1].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.TinyInt ) );

            Assert.That( sp.Parameters[2].IsOutput, Is.True );
            Assert.That( sp.Parameters[2].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[2].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[2].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[2].Variable.Identifier.Name, Is.EqualTo( "@p3" ) );
            Assert.That( sp.Parameters[2].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.SmallInt ) );

            Assert.That( sp.Parameters[3].IsOutput, Is.False );
            Assert.That( sp.Parameters[3].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[3].DefaultValue.ToString(), Is.EqualTo( "= N'Murfn...'" ) );
            Assert.That( sp.Parameters[3].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[3].Variable.Identifier.Name, Is.EqualTo( "@p4" ) );
            Assert.That( sp.Parameters[3].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.NVarChar ) );
            Assert.That( sp.Parameters[3].Variable.TypeDecl.ActualType.SyntaxSize, Is.EqualTo( 50 ) );

            Assert.That( sp.Parameters[4].IsOutput, Is.True );
            Assert.That( sp.Parameters[4].IsInputOutput, Is.True );
            Assert.That( sp.Parameters[4].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[4].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[4].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[4].Variable.Identifier.Name, Is.EqualTo( "@p5" ) );
            Assert.That( sp.Parameters[4].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.VarChar ) );
            Assert.That( sp.Parameters[4].Variable.TypeDecl.ActualType.SyntaxSize, Is.EqualTo( -1 ), "Size is max." );

            Assert.That( sp.Parameters[5].IsOutput, Is.True );
            Assert.That( sp.Parameters[5].IsInputOutput, Is.True );
            Assert.That( sp.Parameters[5].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[5].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[5].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[5].Variable.Identifier.Name, Is.EqualTo( "@p6" ) );
            Assert.That( sp.Parameters[5].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.Char ) );
            Assert.That( sp.Parameters[5].Variable.TypeDecl.ActualType.SyntaxSize, Is.EqualTo( 0 ), "Size is undefined." );

            Assert.That( sp.Parameters[6].IsOutput, Is.True );
            Assert.That( sp.Parameters[6].IsInputOutput, Is.False, "--input behind the comma..." );
            Assert.That( sp.Parameters[6].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[6].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[6].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[6].Variable.Identifier.Name, Is.EqualTo( "@p7" ) );
            Assert.That( sp.Parameters[6].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.Xml ) );
            Assert.That( sp.Parameters[6].Variable.TypeDecl.ActualType.SyntaxSize, Is.EqualTo( -2 ), "Size does not apply." );

            Assert.That( sp.Parameters[7].IsOutput, Is.True );
            Assert.That( sp.Parameters[7].IsInputOutput, Is.True, "-- input on the line above." );
            Assert.That( sp.Parameters[7].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[7].DefaultValue, Is.Null );
            Assert.That( sp.Parameters[7].Variable.Identifier.IsVariable, Is.True );
            Assert.That( sp.Parameters[7].Variable.Identifier.Name, Is.EqualTo( "@p8" ) );
            Assert.That( sp.Parameters[7].Variable.TypeDecl.ActualType.DbType, Is.EqualTo( SqlDbType.SmallDateTime ) );
            Assert.That( sp.Parameters[7].Variable.TypeDecl.ActualType.SyntaxSize, Is.EqualTo( -2 ), "Size does not apply." );

            Assert.That( sp.Parameters[8].IsOutput, Is.False );
            Assert.That( sp.Parameters[8].IsInputOutput, Is.False );
            Assert.That( sp.Parameters[8].IsReadOnly, Is.False );
            Assert.That( sp.Parameters[8].DefaultValue.IsVariable, Is.False );
            Assert.That( sp.Parameters[8].DefaultValue.IsNull, Is.True );
            Assert.That( sp.Parameters[8].DefaultValue.IsLiteral, Is.False );
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
            Assert.That( statement.ToString(), Is.EqualTo( text ) );
            return s;
        }

    }
}
