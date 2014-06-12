using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.SqlServer;

namespace CK.SqlServer.Parser.Tests
{
    [TestFixture]
    [Category( "SqlTokenizer" )]
    public class SqlTokenizerTest
    {
        [Test]
        public void SimpleTokens()
        {
            var s = "1 = 1 and 0 = 0 and 2 = 2";
            SqlTokenizer t = new SqlTokenizer();
            var e = t.Parse( s ).GetEnumerator();
            Assert.That( e.MoveNext() && (e.Current.TokenType & SqlTokenType.IsNumber) != 0 && e.Current.ToString() == "1" );
            Assert.That( e.MoveNext() && e.Current.TokenType == SqlTokenType.Equal && e.Current.ToString() == "=" );
            Assert.That( e.MoveNext() && (e.Current.TokenType & SqlTokenType.IsNumber) != 0 && e.Current.ToString() == "1" );
            Assert.That( e.MoveNext() && e.Current.TokenType == SqlTokenType.And && e.Current.ToString() == "and" );
            Assert.That( e.MoveNext() && (e.Current.TokenType & SqlTokenType.IsNumber) != 0 && e.Current.ToString() == "0" );
            Assert.That( e.MoveNext() && e.Current.TokenType == SqlTokenType.Equal && e.Current.ToString() == "=" );
            Assert.That( e.MoveNext() && (e.Current.TokenType & SqlTokenType.IsNumber) != 0 && e.Current.ToString() == "0" );
            Assert.That( e.MoveNext() && e.Current.TokenType == SqlTokenType.And && e.Current.ToString() == "and" );
            Assert.That( e.MoveNext() && (e.Current.TokenType & SqlTokenType.IsNumber) != 0 && e.Current.ToString() == "2" );
            Assert.That( e.MoveNext() && e.Current.TokenType == SqlTokenType.Equal && e.Current.ToString() == "=" );
            Assert.That( e.MoveNext() && (e.Current.TokenType & SqlTokenType.IsNumber) != 0 && e.Current.ToString() == "2" );
            Assert.That( e.MoveNext() && e.Current.TokenType == SqlTokenType.EndOfInput );
            Assert.That( !e.MoveNext() );
        }

        [Test]
        public void ToStringHelper()
        {
            SqlTokenizer p = new SqlTokenizer();
            Assert.That( p.ToString(), Is.EqualTo( "<no input>" ) );

            p.Reset( "a" );
            Assert.That( p.ToString( 1 ), Is.EqualTo( "a[[HEAD]]" ) );
            p.Forward();
            Assert.That( p.ToString( 20 ), Is.EqualTo( "a[[HEAD]]" ) );
            
            p.Reset( "aa bb cc dd" );
            Assert.That( p.ToString( 1 ), Is.EqualTo( "... [[HEAD]]..." ) );
            Assert.That( p.ToString( 2 ), Is.EqualTo( "...a [[HEAD]]..." ) );
            Assert.That( p.ToString( 3 ), Is.EqualTo( "aa [[HEAD]]..." ) );
            Assert.That( p.ToString( 4 ), Is.EqualTo( "aa [[HEAD]]b..." ) );
            p.Forward();
            Assert.That( p.ToString( 1 ), Is.EqualTo( "... [[HEAD]]..." ) );
            Assert.That( p.ToString( 2 ), Is.EqualTo( "...b [[HEAD]]..." ) );
            Assert.That( p.ToString( 3 ), Is.EqualTo( "...bb [[HEAD]]..." ) );
            Assert.That( p.ToString( 4 ), Is.EqualTo( "... bb [[HEAD]]..." ) );
            Assert.That( p.ToString( 5 ), Is.EqualTo( "...a bb [[HEAD]]..." ) );
            Assert.That( p.ToString( 6 ), Is.EqualTo( "aa bb [[HEAD]]..." ) );
            Assert.That( p.ToString( 7 ), Is.EqualTo( "aa bb [[HEAD]]c..." ) );
            Assert.That( p.ToString( 8 ), Is.EqualTo( "aa bb [[HEAD]]cc..." ) );
            Assert.That( p.ToString( 9 ), Is.EqualTo( "aa bb [[HEAD]]cc ..." ) );
            Assert.That( p.ToString( 10 ), Is.EqualTo( "aa bb [[HEAD]]cc d..." ) );
            Assert.That( p.ToString( 11 ), Is.EqualTo( "aa bb [[HEAD]]cc dd" ) );
            Assert.That( p.ToString( 1000 ), Is.EqualTo( "aa bb [[HEAD]]cc dd" ) );
            p.Forward();
            p.Forward();
            Assert.That( p.ToString( 1 ), Is.EqualTo( "...d[[HEAD]]" ) );
            Assert.That( p.ToString( 2 ), Is.EqualTo( "...dd[[HEAD]]" ) );
            p.Forward();
            Assert.That( p.ToString( 3 ), Is.EqualTo( "... dd[[HEAD]]" ) );
            Assert.That( p.ToString( 4 ), Is.EqualTo( "...c dd[[HEAD]]" ) );
            Assert.That( p.ToString( 11 ), Is.EqualTo( "aa bb cc dd[[HEAD]]" ) );
        }
        
        [Test]
        public void EmptyInputAndComments()
        {
            SqlTokenizer p = new SqlTokenizer();

            p.Reset( "" );
            IsEndOfInput( p );

            p.Reset( "\r\n\t " );
            IsEndOfInput( p );
            CollectionAssert.AreEquivalent( p.Token.LeadingTrivia.Select( t => t.Text ), new[] { "\r\n\t " } );
            CollectionAssert.IsEmpty( p.Token.TrailingTrivia );

            p.Reset( "\r\n\t  --Comment\r\n \t\r\n /*Other\r\nComment...*/ \r\n" );
            IsEndOfInput( p );
            CollectionAssert.AreEquivalent( p.Token.LeadingTrivia.Select( t => t.Text ), new[] { "\r\n\t  ", "Comment", " \t\r\n ", "Other\r\nComment...", " \r\n" } );
            CollectionAssert.IsEmpty( p.Token.TrailingTrivia );
        }

        private static void IsEndOfInput( SqlTokenizer p )
        {
            Assert.That( p.Token is SqlTokenError );
            Assert.That( p.Token.TokenType, Is.EqualTo( SqlTokenType.EndOfInput ) );
            Assert.That( ((SqlTokenError)p.Token).IsEndOfInput );
            Assert.That( !p.Forward() );
            Assert.That( p.Token is SqlTokenError );
            Assert.That( p.Token.TokenType, Is.EqualTo( SqlTokenType.EndOfInput ) );
            Assert.That( ((SqlTokenError)p.Token).IsEndOfInput );
        }

        [Test]
        public void TokenExplainBasic()
        {
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierStandard ), Is.EqualTo( "identifier" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierQuoted ), Is.EqualTo( "\"quoted identifier\"" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierQuotedBracket ), Is.EqualTo( "[quoted identifier]" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierVariable ), Is.EqualTo( "@var" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierReserved ), Is.EqualTo( "reserved" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierReservedStatement ), Is.EqualTo( "statement" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierStandardStatement ), Is.EqualTo( "statement" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierSpecial ), Is.EqualTo( "identifier-special" ) );
        
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierStar ), Is.EqualTo( "*" ) );

            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierTypeXml ), Is.EqualTo( "Xml" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierTypeInt ), Is.EqualTo( "Int" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierTypeVarChar ), Is.EqualTo( "VarChar" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.IdentifierTypeDateTime ), Is.EqualTo( "DateTime" ) );

            Assert.That( SqlTokenizer.Explain( SqlTokenType.String ), Is.EqualTo( "'string'" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.UnicodeString ), Is.EqualTo( "N'unicode string'" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.StarComment ), Is.EqualTo( "/* ... */" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.LineComment ), Is.EqualTo( "-- ..." + Environment.NewLine ) );

            Assert.That( SqlTokenizer.Explain( SqlTokenType.Integer ), Is.EqualTo( "42" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.Float ), Is.EqualTo( "6.02214129e+23" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.Binary ), Is.EqualTo( "0x00CF12A4" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.Decimal ), Is.EqualTo( "124.587" ) );
            Assert.That( SqlTokenizer.Explain( SqlTokenType.Money ), Is.EqualTo( "$548.7" ) );
        }

        [Test]
        public void TokenExplain()
        {
            SqlTokenizer p = new SqlTokenizer();
            string s = @"create table [a.b] . tC ( TheName nvarchar ( 1254 ) ) ;
/* Comment is trivia
(skipped)*/
create procedure [a.b] . [sSP] ( @X int , @Y int ) 
as
begin
  declare @g nvarchar ( 42 ) = N'Oups' ;
  exec [a.b] . sOther @p = @X ;
end";
            p.Reset( s );

            StringBuilder b  = new StringBuilder();
            while( !p.IsErrorOrEndOfInput )
            {
                if( b.Length > 0 ) b.Append( ' ' );
                b.Append( SqlTokenizer.Explain( p.Token.TokenType ) );
                p.Forward();
            }
            string recompose = b.ToString();

            s = s.Replace( "[a.b]", "[quoted identifier]" )
                .Replace( "tC", "identifier" )
                .Replace( "TheName", "identifier" )
                .Replace( "1254", "42" )
                .Replace( "@X", "@var" )
                .Replace( "@Y", "@var" )
                .Replace( "@g", "@var" )
                .Replace( "@p", "@var" )
                .Replace( "[sSP]", "[quoted identifier]" )
                .Replace( "N'Oups'", "N'unicode string'" )
                .Replace( "sOther", "identifier" );

            // Whitespace
            s = s.Replace( "/* Comment is trivia\r\n(skipped)*/", "" )
                .Replace( "\r\n", " " )
                .Replace( "  ", " " )
                .Replace( "  ", " " )
                .Replace( "  ", " " );

            // Keywords & type
            s = s.Replace( "create", "statement" )
                .Replace( "table", "reserved" )
                .Replace( "nvarchar", "NVarChar" )
                .Replace( "int", "Int" )
                .Replace( "procedure", "reserved" )
                .Replace( "as", "reserved" )
                .Replace( "begin", "statement" )
                .Replace( "declare", "statement" )
                .Replace( "exec", "statement" )
                .Replace( "end", "statement" );

            Assert.That( recompose, Is.EqualTo( s ) );
        }

        [Test]
        public void Rewriting()
        {
            SqlTokenizer p = new SqlTokenizer();
            string s = @"create table [a.b] . tC ( TheName nvarchar ( 1254 ) ) ;
/* Comment is trivia
(not skipped)*/
create procedure [a.b].[sSP]( @X int, @Y int ) 
as
begin
  declare @g nvarchar ( 42 ) = N'Oups'; -- End of line comment...
  exec [a.b].sOther @p = @X, @v = $1235.12;
  declare @x1 decimal = .34;
  declare @x2 float = .45e12;
end";
            StringBuilder b  = new StringBuilder();
            foreach( var t in p.ParseWithoutError( s ) ) t.Write( b );
            string s2 = b.ToString();

            // Fix: .34 is changed as 0.34 (decimal), .45e12 becomes 0.45e12 (float).
            Assert.That( s2, Is.EqualTo( s.Replace( ".34", "0.34" ).Replace( ".45e12", "0.45e12" ) ) );
        }

        [Test]
        public void IdentifiersAndTerminal()
        {
            SqlTokenizer p = new SqlTokenizer();
            SqlToken t;
            SqlTokenIdentifier tU;
            t = p.ParseWithoutError( "IdentifierStandard" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierStandard );
            Assert.That( t is SqlTokenIdentifier );
            Assert.That( ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( false ), Is.SameAs( t ) );
            Assert.That( t.ToString(), Is.EqualTo( "IdentifierStandard" ) );

            t = p.ParseWithoutError( "[IdentifierQuotedBracket]" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierQuotedBracket );
            Assert.That( t is SqlTokenIdentifier );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( true );
            Assert.That( tU, Is.Not.SameAs( t ) );
            Assert.That( t.ToString(), Is.EqualTo( "[IdentifierQuotedBracket]" ) );
            Assert.That( tU.ToString(), Is.EqualTo( "IdentifierQuotedBracket" ) );

            t = p.ParseWithoutError( "[Identifier Quoted Bracket]" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierQuotedBracket );
            Assert.That( t is SqlTokenIdentifier );
            Assert.That( t.ToString(), Is.EqualTo( "[Identifier Quoted Bracket]" ) );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( true );
            Assert.That( tU, Is.SameAs( t ) );

            t = p.ParseWithoutError( "LiKE" ).ElementAt( 0 );
            Assert.That( t is SqlTokenIdentifier );
            Assert.That( t.ToString(), Is.EqualTo( "LiKE" ) );
            Assert.That( t.TokenType == SqlTokenType.Like );

            t = p.ParseWithoutError( "[LiKE]" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierQuotedBracket );
            Assert.That( t is SqlTokenIdentifier );
            Assert.That( t.ToString(), Is.EqualTo( "[LiKE]" ) );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( true );
            Assert.That( tU, Is.SameAs( t ) );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( false );
            Assert.That( tU, Is.Not.SameAs( t ) );
            Assert.That( tU.ToString(), Is.EqualTo( "LiKE" ) );
            Assert.That( tU.TokenType, Is.EqualTo( SqlTokenType.Like ) );

            t = p.ParseWithoutError( "IN" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.In );
            Assert.That( t is SqlTokenIdentifier );
            Assert.That( t.ToString(), Is.EqualTo( "IN" ) );

            t = p.ParseWithoutError( "[IN]" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierQuotedBracket );
            Assert.That( t is SqlTokenIdentifier );
            Assert.That( t.ToString(), Is.EqualTo( "[IN]" ) );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( true );
            Assert.That( tU, Is.SameAs( t ) );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( false );
            Assert.That( tU, Is.Not.SameAs( t ) );
            Assert.That( tU.ToString(), Is.EqualTo( "IN" ) );
            Assert.That( tU.TokenType, Is.EqualTo( SqlTokenType.In ) );

            t = p.ParseWithoutError( "int" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierTypeInt );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( true );
            Assert.That( tU, Is.SameAs( t ) );

            t = p.ParseWithoutError( "[int]" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierQuotedBracket );
            Assert.That( t is SqlTokenIdentifier );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( true );
            Assert.That( tU, Is.Not.SameAs( t ) );
            Assert.That( tU.ToString(), Is.EqualTo( "int" ) );
            Assert.That( tU.TokenType == SqlTokenType.IdentifierTypeInt );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( false );
            Assert.That( tU, Is.Not.SameAs( t ) );
            Assert.That( tU.ToString(), Is.EqualTo( "int" ) );
            Assert.That( tU.TokenType == SqlTokenType.IdentifierTypeInt );

            t = p.ParseWithoutError( @"""smalliNt""" ).ElementAt( 0 );
            Assert.That( t.TokenType == SqlTokenType.IdentifierQuoted );
            Assert.That( t is SqlTokenIdentifier );
            Assert.That( t.ToString(), Is.EqualTo( @"""smalliNt""" ) );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( true );
            Assert.That( tU, Is.Not.SameAs( t ) );
            Assert.That( tU.ToString(), Is.EqualTo( "smalliNt" ) );
            Assert.That( tU.TokenType == SqlTokenType.IdentifierTypeSmallInt );
            tU = ((SqlTokenIdentifier)t).RemoveQuoteIfPossible( false );
            Assert.That( tU, Is.Not.SameAs( t ) );
            Assert.That( tU.ToString(), Is.EqualTo( "smalliNt" ) );
            Assert.That( tU.TokenType == SqlTokenType.IdentifierTypeSmallInt );

        }

        [Test]
        public void Numbers()
        {
            SqlTokenizer p = new SqlTokenizer();

            AssertRewrite( p, "0", "0" );
            AssertRewrite( p, "0.23", "0.23" );
            AssertRewrite( p, ".23", "0.23" );
            AssertRewrite( p, "0000.23", "0.23" );

            AssertRewrite( p, "$", "$0" );
            //AssertRewrite( p, "$1", "$1" );
            AssertRewrite( p, "$1.", "$1.0" );
            AssertRewrite( p, "£.233", "£0.233" );
            AssertRewrite( p, "£    .23", "£0.23" );
            AssertRewrite( p, "$   0000.23", "$0.23" );

            AssertRewrite( p, ".45E+12", "0.45e12" );
            AssertRewrite( p, "00012.147e-4", "12.147e-4" );
        }

        static void AssertRewrite( SqlTokenizer p, string toParse, string rewritten )
        {
            StringBuilder b  = new StringBuilder();
            foreach( var t in p.ParseWithoutError( toParse ) ) t.Write( b );
            string r = b.ToString();
            Assert.That( r, Is.EqualTo( rewritten ) );
        }

        [Test]
        public void CommentsAreTrivias()
        {
            string s = @"'' -- CancelDate";
            SqlTokenizer p = new SqlTokenizer();
            var tokens = p.Parse( s ).ToArray();
            Assert.That( tokens.Length == 2 );
            Assert.That( tokens[0].TokenType == SqlTokenType.String );
            Assert.That( tokens[0].TrailingTrivia.Count == 2 );
            Assert.That( tokens[0].TrailingTrivia[0].TokenType == SqlTokenType.None );
            Assert.That( tokens[0].TrailingTrivia[0].Text == " " );
            Assert.That( tokens[0].TrailingTrivia[1].TokenType == SqlTokenType.LineComment );
            Assert.That( tokens[0].TrailingTrivia[1].Text == " CancelDate" );
            Assert.That( tokens[1].TokenType == SqlTokenType.EndOfInput );
        }
        
        [Test]
        public void LineCommentsEatsItsPrefixAndLineTermination()
        {
            string s = @"'' -- CancelDate
TOKEN";
            SqlTokenizer p = new SqlTokenizer();
            var tokens = p.Parse( s ).ToArray();
            Assert.That( tokens.Length == 3 );
            Assert.That( tokens[0].TokenType == SqlTokenType.String );
            Assert.That( tokens[0].TrailingTrivia.Count == 2 );
            Assert.That( tokens[0].TrailingTrivia[0].TokenType == SqlTokenType.None );
            Assert.That( tokens[0].TrailingTrivia[0].Text == " " );
            Assert.That( tokens[0].TrailingTrivia[1].TokenType == SqlTokenType.LineComment );
            Assert.That( tokens[0].TrailingTrivia[1].Text == " CancelDate", "No line endings into it." );
            Assert.That( tokens[1].TokenType == SqlTokenType.IdentifierStandard );
            Assert.That( tokens[1].LeadingTrivia.Count == 0 );
            Assert.That( tokens[1].TrailingTrivia.Count == 0 );
            Assert.That( tokens[2].TokenType == SqlTokenType.EndOfInput );
        }
    }

}
