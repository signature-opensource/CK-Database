#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Parser.Tests\Parsing\SqlTokenReaderTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CK.SqlServer.Parser.Tests.Parsing
{
    [TestFixture]
    public class SqlTokenReaderTests
    {
        static SqlTokenReader CreateReader( string s )
        {
            SqlTokenizer t = new SqlTokenizer();
            var r = new SqlTokenReader( t.Parse( s ), t.ToString );
            r.MoveNext();
            return r;
        }

        [Test]
        public void TokenTypes()
        {
            {
                SqlTokenReader r = CreateReader( "as" );
                Assert.That( r.Current.TokenType == SqlTokenType.As );
                SqlTokenIdentifier asToken;
                Assert.That( r.IsToken( out asToken, SqlTokenType.As, true ) );
            }
            {
                SqlTokenReader r = CreateReader( "[as]" );
                Assert.That( r.Current.TokenType == SqlTokenType.As, Is.False );
                SqlTokenIdentifier asToken;
                Assert.That( r.IsToken( out asToken, SqlTokenType.As, expected: true ), Is.False );
                Assert.That( r.IsError );
            }
        }
    }
}
