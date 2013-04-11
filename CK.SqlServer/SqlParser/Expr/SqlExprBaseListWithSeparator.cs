using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseListWithSeparator<T> : SqlExpr, ISqlExprEnclosable where T : IAbstractExpr
    {
        readonly IAbstractExpr[] _components;

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparator{T}"/> of <typeparamref name="T"/> enclosed in a <see cref="SqlTokenOpenPar"/> and a <see cref="SqlTokenClosePar"/> 
        /// and with <paramref name="validSeparator"/> that is to <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseListWithSeparator( SqlTokenOpenPar openPar, IList<IAbstractExpr> exprOrTokens, SqlTokenClosePar closePar, bool allowEmpty, Predicate<IAbstractExpr> validSeparator = null )
        {
            if( openPar == null ) throw new ArgumentNullException( "openPar" );
            if( exprOrTokens == null ) throw new ArgumentNullException( "exprOrTokens" );
            if( closePar == null ) throw new ArgumentNullException( "closePar" );
            _components = CreateArray( openPar, exprOrTokens, exprOrTokens.Count, closePar );
            CheckArray( _components, allowEmpty, true, true, validSeparator ?? IsCommaSeparator );
        }

        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseListWithSeparator{T}"/> of <typeparamref name="T"/> without <see cref="Opener"/> nor <see cref="Closer"/> 
        /// and with <paramref name="validSeparator"/> that is to <see cref="IsCommaSeparator"/> by default.
        /// </summary>
        /// <param name="exprOrTokens">List of tokens or expressions.</param>
        /// <param name="validSeparator">Defaults to a predicate that checks that separators are commas (see <see cref="IsCommaSeparator"/>).</param>
        public SqlExprBaseListWithSeparator( IList<IAbstractExpr> exprOrTokens, bool allowEmpty, Predicate<IAbstractExpr> validSeparator = null )
        {
            if( exprOrTokens == null ) throw new ArgumentNullException( "exprOrTokens" );
            _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, exprOrTokens, 0, exprOrTokens.Count, SqlExprMultiToken<SqlTokenClosePar>.Empty );
            CheckArray( _components, allowEmpty, true, false, validSeparator ?? IsCommaSeparator );
        }

        internal SqlExprBaseListWithSeparator( IAbstractExpr[] components )
        {
            Debug.Assert( components != null );
            _components = components;
        }

        internal IAbstractExpr[] EncloseComponents( SqlTokenOpenPar opener, SqlTokenClosePar closer )
        {
            return CreateArray( opener, _components, closer );
        }

        public SqlExprMultiToken<SqlTokenOpenPar> Opener
        {
            get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; }
        }

        public SqlExprMultiToken<SqlTokenClosePar> Closer
        {
            get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[_components.Length - 1]; }
        }

        /// <summary>
        /// Gets whether this can actually be enclosed by a new pair of parenthesis.
        /// </summary>
        public abstract bool CanEnclose { get; }

        /// <summary>
        /// Creates a new enclosable that is enclosed by new pair(s) of parenthesis.
        /// Throws an <see cref="InvalidOperationException"/> if <see cref="CanEnclose"/> is false.
        /// </summary>
        /// <param name="opener">New opening parenthesis.</param>
        /// <param name="closer">New closing parenthesis.</param>
        /// <returns>A clone of this object enclosed by the new tokens.</returns>
        public abstract ISqlExprEnclosable Enclose( SqlTokenOpenPar opener, SqlTokenClosePar closer );

        /// <summary>
        /// Gets the content (items and separators) without <see cref="Opener"/> nor <see cref="Closer"/>.
        /// </summary>
        public IEnumerable<IAbstractExpr> ComponentsWithoutParenthesis { get { return _components.Skip( 1 ).Take( _components.Length - 2 ); } }

        /// <summary>
        /// Gets all the components of this list (opener, items with separators and closer).
        /// </summary>
        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        public int SeparatorCount { get { return _components.Length / 2 - 1; } }

        /// <summary>
        /// Gets the separators token.
        /// </summary>
        public IEnumerable<IAbstractExpr> SeparatorTokens { get { return ComponentsWithoutParenthesis.Skip( 1 ).Where( ( x, i ) => i % 2 != 0 ); } }

        protected IAbstractExpr SeparatorTokenAt( int i )
        {
            return _components[(i+1) * 2];
        }

        protected int NonSeparatorCount { get { return (_components.Length + 1) / 2 - 1; } }

        protected IEnumerable<T> NonSeparatorTokens { get { return ComponentsWithoutParenthesis.Where( ( x, i ) => i % 2 == 0 ).Cast<T>(); } }

        protected T NonSeparatorTokenAt( int i )
        {
            return (T)_components[i* 2+1];
        }

        [Conditional("DEBUG")]
        protected static void DebugCheckArray( IAbstractExpr[] t, bool allowEmpty, bool hasOpenerAndCloser, bool atLeastOneOpener, Predicate<IAbstractExpr> validSeparator )
        {
            CheckArray( t, allowEmpty, hasOpenerAndCloser, atLeastOneOpener, validSeparator );
        }

        internal static void CheckArray( IAbstractExpr[] t, bool allowEmpty, bool hasOpenerAndCloser, bool atLeastOneOpener, Predicate<IAbstractExpr> validSeparator )
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

        internal static string BuildArray( IEnumerator<IAbstractExpr> tokens, bool allowEmpty, Predicate<IAbstractExpr> validSeparator, string elementName, out IAbstractExpr[] result )
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
                    all.Add( separator );
                    all.Add( element );
                }
            }
            if( all.Count == 0 && !allowEmpty ) return String.Format( "Expected {0}.", elementName );
            result = all.ToArray();
            return null;
        }

        protected IAbstractExpr[] ReplaceNonSeparator( Func<T, IAbstractExpr> replacer )
        {
            return ReplaceNonSeparator( _components, true, replacer );
        }

        internal static IAbstractExpr[] ReplaceNonSeparator( IAbstractExpr[] t, bool hasOpenerAndCloser, Func<T, IAbstractExpr> replacer )
        {
            IAbstractExpr[] modified = null;
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
                IAbstractExpr r = replacer( o );
                if( !ReferenceEquals( r, o ) )
                {
                    if( modified == null ) modified = (IAbstractExpr[])t.Clone();
                    modified[i] = r;
                }
            }
            return modified;
        }


    }

}
