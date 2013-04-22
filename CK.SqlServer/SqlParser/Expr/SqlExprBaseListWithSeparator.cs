using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseListWithSeparator<T> : SqlExpr where T : ISqlItem
    {
        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparator{T}"/> of <typeparamref name="T"/> enclosed in a <see cref="SqlTokenOpenPar"/> and a <see cref="SqlTokenClosePar"/> 
        /// and with <paramref name="validSeparator"/> that is to <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseListWithSeparator( SqlTokenOpenPar openPar, IList<ISqlItem> exprOrTokens, SqlTokenClosePar closePar, bool allowEmpty, Predicate<ISqlItem> validSeparator = null )
            : this( Build( openPar, exprOrTokens, closePar, allowEmpty, validSeparator ) )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparator{T}"/> of <typeparamref name="T"/> without <see cref="Opener"/> nor <see cref="Closer"/> 
        /// and with <paramref name="validSeparator"/> that is set to <see cref="SqlToken.IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="SqlToken.IsCommaSeparator"/>).</param>
        public SqlExprBaseListWithSeparator( IList<ISqlItem> exprOrTokens, bool allowEmpty, Predicate<ISqlItem> validSeparator = null )
            : this( Build( exprOrTokens, allowEmpty, validSeparator ) )
        {
        }

        static ISqlItem[] Build( SqlTokenOpenPar openPar, IList<ISqlItem> exprOrTokens, SqlTokenClosePar closePar, bool allowEmpty, Predicate<ISqlItem> validSeparator = null )
        {
            if( openPar == null ) throw new ArgumentNullException( "openPar" );
            if( exprOrTokens == null ) throw new ArgumentNullException( "exprOrTokens" );
            if( closePar == null ) throw new ArgumentNullException( "closePar" );
            var c = CreateArray( openPar, exprOrTokens, exprOrTokens.Count, closePar );
            CheckArray( c, allowEmpty, true, true, validSeparator ?? SqlToken.IsCommaSeparator );
            return c;
        }

        static ISqlItem[] Build( IList<ISqlItem> exprOrTokens, bool allowEmpty, Predicate<ISqlItem> validSeparator = null )
        {
            if( exprOrTokens == null ) throw new ArgumentNullException( "exprOrTokens" );
            var c = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, exprOrTokens, 0, exprOrTokens.Count, SqlExprMultiToken<SqlTokenClosePar>.Empty );
            CheckArray( c, allowEmpty, true, false, validSeparator ?? SqlToken.IsCommaSeparator );
            return c;
        }

        internal SqlExprBaseListWithSeparator( ISqlItem[] components )
            : base( components )
        {
        }

        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        public int SeparatorCount { get { return Slots.Length / 2 - 1; } }

        /// <summary>
        /// Gets the separators token.
        /// </summary>
        public IEnumerable<ISqlItem> SeparatorTokens { get { return ItemsWithoutParenthesis.Skip( 1 ).Where( ( x, i ) => i % 2 != 0 ); } }

        protected ISqlItem SeparatorTokenAt( int i ) { return Slots[(i+1) * 2]; }

        protected int NonSeparatorCount { get { return (Slots.Length + 1) / 2 - 1; } }

        protected IEnumerable<T> NonSeparatorTokens { get { return ItemsWithoutParenthesis.Where( ( x, i ) => i % 2 == 0 ).Cast<T>(); } }

        protected T NonSeparatorTokenAt( int i ) { return (T)Slots[i* 2+1]; }

        [Conditional("DEBUG")]
        protected static void DebugCheckArray( ISqlItem[] t, bool allowEmpty, bool hasOpenerAndCloser, bool atLeastOneOpener, Predicate<ISqlItem> validSeparator )
        {
            CheckArray( t, allowEmpty, hasOpenerAndCloser, atLeastOneOpener, validSeparator );
        }

        internal static void CheckArray( ISqlItem[] t, bool allowEmpty, bool hasOpenerAndCloser, bool atLeastOneOpener, Predicate<ISqlItem> validSeparator )
        {
            int len = t.Length;
            int offset = 0;
            if( hasOpenerAndCloser )
            {
                len -= 2;
                offset = 1;
                if( len < 0 ) throw new ArgumentException( "There must be at least the opener/closer pair.", "tokens" );
                SqlExprMultiToken<SqlTokenOpenPar> opener = t[0] as SqlExprMultiToken<SqlTokenOpenPar>;
                SqlExprMultiToken<SqlTokenClosePar> closer = t[t.Length - 1] as SqlExprMultiToken<SqlTokenClosePar>;
                if( opener == null || closer == null ) throw new ArgumentException( "Opener/Closer not found.", "tokens" );
                if( opener.Count != closer.Count ) throw new ArgumentException( "Opener/Closer are not balanced.", "tokens" );
                if( atLeastOneOpener && opener.Count == 0 ) throw new ArgumentException( "There must be at least one parenthesis.", "tokens" );
            }
            if( (len % 2) == 0 && (len != 0 || !allowEmpty) ) throw new ArgumentException( "There must be an odd number of elements.", "tokens" );
            len = (len + 1) / 2;
            for( int i = 0; i < len; ++i )
            {
                if( !(t[i * 2 + offset] is T) )
                {
                    throw new ArgumentException( String.Format( "Invalid token at {0}. It must be {1}.", i * 2, typeof( T ).Name ), "tokens" );
                }
                if( validSeparator != null && i > 0 )
                {
                    if( !validSeparator( t[i * 2 - 1 + offset] ) )
                    {
                        throw new ArgumentException( String.Format( "Invalid separator at {0}.", i * 2 - 1, typeof( T ).Name ), "tokens" );
                    }
                }
            }
        }

        internal static string BuildArray( IEnumerator<ISqlItem> tokens, bool allowEmpty, Predicate<ISqlItem> validSeparator, string elementName, out ISqlItem[] result )
        {
            Debug.Assert( tokens != null );
            result = null;
            List<ISqlItem> all = new List<ISqlItem>();
            ISqlItem element = tokens.Current;
            if( element is T )
            {
                all.Add( element );
                ISqlItem separator;
                while( tokens.MoveNext() && validSeparator( separator = tokens.Current ) )
                {
                    if( !tokens.MoveNext() || !((element = tokens.Current) is T) )
                    {
                        return String.Format( "Missing {0} after {1}.", elementName, separator.ToString() );
                    }
                    all.Add( separator );
                    all.Add( element );
                }
            }
            if( all.Count == 0 && !allowEmpty ) return String.Format( "Expected {0}.", elementName );
            result = all.ToArray();
            return null;
        }

        protected ISqlItem[] ReplaceNonSeparator( Func<T, ISqlItem> replacer )
        {
            return ReplaceNonSeparator( Slots, true, replacer );
        }

        internal static ISqlItem[] ReplaceNonSeparator( ISqlItem[] t, bool hasOpenerAndCloser, Func<T, ISqlItem> replacer )
        {
            ISqlItem[] modified = null;
            int len = t.Length;
            int i = 0;
            if( hasOpenerAndCloser )
            {
                len -= 1;
                i = 1;
            }
            for(; i < len; i += 2 )
            {
                var o = (T)t[i];
                ISqlItem r = replacer( o );
                if( !ReferenceEquals( r, o ) )
                {
                    if( modified == null ) modified = (ISqlItem[])t.Clone();
                    modified[i] = r;
                }
            }
            return modified;
        }


    }

}
