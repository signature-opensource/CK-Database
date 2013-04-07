using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
using System.Diagnostics;
using System.Globalization;

namespace CK.SqlServer
{
    /// <summary>
    /// Base class for (non comment) tokens.
    /// </summary>
    public abstract class SqlToken : IAbstractExpr
    {
        public SqlToken( SqlTokenType t, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
        {
            if( t > 0 && (t & (SqlTokenType.TokenDiscriminatorMask & ~SqlTokenType.IsComment)) == 0 ) throw new ArgumentException( "Invalid token type." );
            
            TokenType = t;
            LeadingTrivia = leadingTrivia ?? CKReadOnlyListEmpty<SqlTrivia>.Empty;
            TrailingTrivia = trailingTrivia ?? CKReadOnlyListEmpty<SqlTrivia>.Empty;
        }

        public readonly SqlTokenType TokenType;
        public readonly IReadOnlyList<SqlTrivia> LeadingTrivia;
        public readonly IReadOnlyList<SqlTrivia> TrailingTrivia;

        public void Write( StringBuilder b )
        {
            foreach( var t in LeadingTrivia ) t.Write( b );
            DoWrite( b );
            foreach( var t in TrailingTrivia ) t.Write( b );
        }

        public void WriteWithoutTrivias( StringBuilder b )
        {
            DoWrite( b );
        }

        abstract protected void DoWrite( StringBuilder b );

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            DoWrite( b );
            return b.ToString();
        }

        IEnumerable<SqlToken> IAbstractExpr.Tokens
        {
            get { return new CKReadOnlyListMono<SqlToken>( this ); }
        }
    }

}
