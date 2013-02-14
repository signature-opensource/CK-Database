using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseListWithSeparator<T> : SqlExpr where T : IAbstractExpr
    {
        readonly IAbstractExpr[] _tokens;

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparator"/> of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="exprOrTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas.</param>
        public SqlExprBaseListWithSeparator( IEnumerable<IAbstractExpr> exprOrTokens, Predicate<IAbstractExpr> validSeparator = null )
        {
            _tokens = InitializeArray( exprOrTokens, false, validSeparator ?? (t => (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Comma) );
        }

        static internal IAbstractExpr[] InitializeArray( IEnumerable<IAbstractExpr> tokens, bool allowEmpty, Predicate<IAbstractExpr> validSeparator = null )
        {
            if( tokens == null ) throw new ArgumentNullException( "tokens" );
            IAbstractExpr[] t = tokens.ToArray();
            int len = t.Length;
            if( (len % 2) == 0 && (!allowEmpty && len == 0) ) throw new ArgumentException( "There must be an odd number of elements.", "tokens" );
            len = (len + 1) / 2;
            for( int i = 0; i < len; ++i )
            {
                if( !(t[i * 2] is T) )
                {
                    throw new ArgumentException( String.Format( "Invalid token at {0}. It must be {1}.", i * 2, typeof(T).Name ), "tokens" );
                }
                if( validSeparator != null && i > 0 )
                {
                    if( !validSeparator( t[i*2-1] ) )
                    {
                        throw new ArgumentException( String.Format( "Invalid separator at {0}.", i * 2-1, typeof( T ).Name ), "tokens" );
                    }
                }
            }
            return t;
        }

        public override IEnumerable<SqlToken> Tokens { get { return Flatten( _tokens ); } }

        protected int SeparatorCount { get { return _tokens.Length / 2; } }

        protected IEnumerable<IAbstractExpr> SeparatorTokens { get { return _tokens.Where( ( x, i ) => i % 2 == 1 ); } }

        protected IAbstractExpr SeparatorTokenAt( int i )
        {
            return _tokens[i * 2 + 1];
        }

        protected int NonSeparatorCount { get { return (_tokens.Length + 1) / 2; } }
        
        protected IEnumerable<T> NonSeparatorTokens { get { return _tokens.Where( ( x, i ) => i % 2 == 0 ).Cast<T>(); } }

        protected T NonSeparatorTokenAt( int i )
        {
            return (T)_tokens[i* 2];
        }

    }

}
