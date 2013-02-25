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
        /// Initializes a new <see cref="SqlExprBaseListWithSeparator{T}"/> of <typeparamref name="T"/> with <paramref name="validSeparator"/> sets to <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseListWithSeparator( IEnumerable<IAbstractExpr> exprOrTokens, bool allowEmpty, Predicate<IAbstractExpr> validSeparator = null )
        {
            if( exprOrTokens == null ) throw new ArgumentNullException( "exprOrTokens" );
            IAbstractExpr[] a = exprOrTokens.ToArray();
            CheckArray( a, allowEmpty, validSeparator ?? IsCommaSeparator );
            _tokens = a;
        }

        internal SqlExprBaseListWithSeparator( IAbstractExpr[] tokens )
        {
            Debug.Assert( tokens != null );
            _tokens = tokens;
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is a comma token.
        /// </summary>
        /// <param name="t">Potential comma token.</param>
        /// <returns>Whether the token is a comma or not.</returns>
        static public bool IsCommaSeparator( IAbstractExpr t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Comma;
        }

        /// <summary>
        /// True if the <see cref="IAbstractExpr"/> is a dot token.
        /// </summary>
        /// <param name="t">Potential dot token.</param>
        /// <returns>Whether the token is a comma or not.</returns>
        static public bool IsDotSeparator( IAbstractExpr t )
        {
            return (t is SqlToken) && ((SqlToken)t).TokenType == SqlTokenType.Dot;
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

        [Conditional("DEBUG")]
        protected static void DebugCheckArray( IAbstractExpr[] t, bool allowEmpty, Predicate<IAbstractExpr> validSeparator )
        {
            CheckArray( t, allowEmpty, validSeparator );
        }

        private static void CheckArray( IAbstractExpr[] t, bool allowEmpty, Predicate<IAbstractExpr> validSeparator )
        {
            int len = t.Length;
            if( (len % 2) == 0 && (!allowEmpty && len == 0) ) throw new ArgumentException( "There must be an odd number of elements.", "tokens" );
            len = (len + 1) / 2;
            for( int i = 0; i < len; ++i )
            {
                if( !(t[i * 2] is T) )
                {
                    throw new ArgumentException( String.Format( "Invalid token at {0}. It must be {1}.", i * 2, typeof( T ).Name ), "tokens" );
                }
                if( validSeparator != null && i > 0 )
                {
                    if( !validSeparator( t[i * 2 - 1] ) )
                    {
                        throw new ArgumentException( String.Format( "Invalid separator at {0}.", i * 2 - 1, typeof( T ).Name ), "tokens" );
                    }
                }
            }
        }

        static internal string BuildArray( IEnumerator<IAbstractExpr> tokens, bool allowEmpty, Predicate<IAbstractExpr> validSeparator, string elementName, out IAbstractExpr[] result )
        {
            Debug.Assert( tokens != null );
            result = null;
            List<IAbstractExpr> all = new List<IAbstractExpr>();
            IAbstractExpr element = tokens.Current;
            if( element is T )
            {
                all.Add( element );
                IAbstractExpr separator;
                while( tokens.MoveNext() && validSeparator( separator = tokens.Current ) )
                {
                    if( !tokens.MoveNext() || !((element = tokens.Current) is T) )
                    {
                        return String.Format( "Missing {0} after {1}.", elementName, separator.ToString() );
                    }
                    all.Add( element );
                }
            }
            if( all.Count == 0 && !allowEmpty ) return String.Format( "Expected {0}.", elementName );
            result = all.ToArray();
            return null;
        }

        protected List<IAbstractExpr> ReplaceNonSeparator( Func<T, T> replacer )
        {
            return ReplaceNonSeparator( _tokens, replacer );
        }

        static List<IAbstractExpr> ReplaceNonSeparator( IList<IAbstractExpr> tokens, Func<T, T> replacer )
        {
            bool changed = false;
            List<IAbstractExpr> newT = new List<IAbstractExpr>( tokens.Count );
            for( int i = 0; i < tokens.Count; ++i )
            {
                var o = (T)tokens[i];
                T r = replacer( o );
                if( ReferenceEquals( r, o ) ) newT.Add( r );
                else
                {
                    changed = true;
                    if( r != null ) newT.Add( r );
                    else
                    {
                        ++i;
                        continue;
                    }
                }
                newT.Add( tokens[++i] );
            }
            return changed ? newT : null;
        }

    }

}
