using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprMultiIdentifier : SqlExprBaseListWithSeparatorList<SqlTokenIdentifier>
    {
        public SqlExprMultiIdentifier( IEnumerable<SqlToken> tokens )
            : base( tokens, false, IsDotSeparator )
        {
        }

        internal SqlExprMultiIdentifier( IAbstractExpr[] tokens )
            : base( tokens )
        {
            Debug.Assert( tokens != null );
            DebugCheckArray( tokens, false, IsDotSeparator );
        }

        static internal string BuildArray( IEnumerator<IAbstractExpr> tokens, out IAbstractExpr[] result )
        {
            return BuildArray( tokens, false, IsDotSeparator, "identifier", out result );
        }

        public int IdentifierCount { get { return base.NonSeparatorCount; } }

        public IEnumerable<SqlTokenIdentifier> Identifiers { get { return base.NonSeparatorTokens; } }

        public SqlExprMultiIdentifier RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            SqlExprMultiIdentifier result = this;
            var tokenList = ReplaceNonSeparator( t => t.RemoveQuoteIfPossible( keepIfReservedKeyword ) );
            if( tokenList != null )
            {
                Debug.Assert( tokenList.Count > 0 );
                result = new SqlExprMultiIdentifier( tokenList.ToArray() );
            }
            return this;
        }


        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
