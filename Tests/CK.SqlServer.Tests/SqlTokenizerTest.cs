using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.SqlServer;

namespace CK.Javascript.Tests
{
    [TestFixture]
    public class SqlTokenizerTest
    {
        [Test]
        public void EmptyInputAndComments()
        {
            SqlTokeniser p = new SqlTokeniser();
            p.SkipComments = false;

            p.Reset( "" );
            Assert.That( p.IsEndOfInput );

            p.Reset( "\r\n\t " );
            Assert.That( p.IsEndOfInput );

            p.Reset( "\r\n\t  --Comment\r\n \t\r\n /*Other\r\nComment...*/ \r\n" );
            Assert.That( !p.IsEndOfInput );
            Assert.That( p.IsComment );
            Assert.That( p.ReadComment(), Is.EqualTo( "Comment" ) );
            Assert.That( p.IsComment );
            Assert.That( p.ReadComment(), Is.EqualTo( "Other\r\nComment..." ) );
            Assert.That( p.IsEndOfInput );

            p.SkipComments = true;
            p.Reset( "\r\n\t  --Comment\r\n \t\r\n /*Other\r\nComment/* \r\n" );
            Assert.That( p.IsEndOfInput );

        }

        [Test]
        public void TokenExplain()
        {
            SqlTokeniser p = new SqlTokeniser();
            p.SkipComments = false;

            Assert.That( SqlTokeniser.Explain( SqlToken.Identifier ), Is.EqualTo( "identifier" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.IdentifierQuoted ), Is.EqualTo( "\"quoted identifier\"" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.IdentifierQuotedBracket ), Is.EqualTo( "[quoted identifier]" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.Variable ), Is.EqualTo( "@var" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.Keyword ), Is.EqualTo( "keyword" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.String ), Is.EqualTo( "'string'" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.UnicodeString ), Is.EqualTo( "N'unicode string'" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.StarComment ), Is.EqualTo( "/* ... */" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.LineComment ), Is.EqualTo( "-- ..." + Environment.NewLine ) );

            Assert.That( SqlTokeniser.Explain( SqlToken.Integer ), Is.EqualTo( "42" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.Float ), Is.EqualTo( "6.02214129e+23" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.Binary ), Is.EqualTo( "0x00CF12A4" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.Decimal ), Is.EqualTo( "124.587" ) );
            Assert.That( SqlTokeniser.Explain( SqlToken.Money ), Is.EqualTo( "$548.7" ) );

            string s = @"create table [a.b] . tC ( TheName nvarchar ( 1254 ) ) ;
/* Comment
(not skipped)*/
create procedure [a.b] . [sSP] ( @X int , @Y int ) 
as
begin
  declare @g nvarchar ( 42 ) = N'Oups' ;
  exec [a.b] . sOther @p = @X ;
end";
            p.Reset( s );
            StringBuilder sbR  = new StringBuilder();
            while( !p.IsEndOfInput )
            {
                if( sbR.Length > 0 ) sbR.Append( ' ' );
                sbR.Append( SqlTokeniser.Explain( p.CurrentToken ) );
                p.Forward();
            }
            string recompose = sbR.ToString();

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
            s = s.Replace( "/* Comment\r\n(not skipped)*/", "/* ... */" )
                .Replace( "\r\n", " " )
                .Replace( "  ", " " )
                .Replace( "  ", " " )
                .Replace( "  ", " " );

            // Keywords
            s = s.Replace( "create", "keyword" )
                .Replace( "table", "keyword" )
                .Replace( "nvarchar", "keyword" )
                .Replace( "int", "keyword" )
                .Replace( "procedure", "keyword" )
                .Replace( "as", "keyword" )
                .Replace( "begin", "keyword" )
                .Replace( "declare", "keyword" )
                .Replace( "exec", "keyword" )
                .Replace( "end", "keyword" );

            Assert.That( recompose, Is.EqualTo( s ) );
        }


    }
}
