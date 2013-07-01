using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{

    public class SqlExprMultiIdentifier : SqlExpr, ISqlIdentifier
    {
        /// <summary>
        /// Initializes a new <see cref="SqlExprMultiIdentifier"/> that may be enclosed or not. 
        /// Separator is <see cref="IsDotOrDoubleColonSeparator"/>.
        /// </summary>
        /// <param name="isEnclosed">Whether given tokens are enclosed or not.</param>
        /// <param name="tokens">Identifiers and separator tokens. It may be enclosed or not.</param>
        public SqlExprMultiIdentifier( bool isEnclosed, IList<ISqlItem> tokens )
            : this( Build( isEnclosed, tokens ) )
        {
        }

        static ISqlItem[] Build( bool isEnclosed, IList<ISqlItem> tokens )
        {
            if( tokens.Count == 0 ) throw new ArgumentException();
            ISqlItem[] r;
            if( isEnclosed ) r = tokens.ToArray();
            else r = CreateEnclosedArray( tokens.AsReadOnlyList() );
            SqlExprBaseListWithSeparator<SqlTokenIdentifier>.CheckArray( r, false, true, false, SqlToken.IsDotOrDoubleColonSeparator );
            return r;
        }

        internal SqlExprMultiIdentifier( ISqlItem[] slots )
            : base( slots )
        {
        }

        static internal string BuildArray( IEnumerator<ISqlItem> tokens, out ISqlItem[] result, SqlTokenIdentifier firstForLookup = null )
        {
            return SqlExprBaseListWithSeparator<SqlTokenIdentifier>.BuildArray( tokens, false, SqlToken.IsDotOrDoubleColonSeparator, "identifier", out result, firstForLookup );
        }

        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        public int SeparatorCount { get { return (Slots.Length / 2) - 1; } }

        /// <summary>
        /// Gets the separators token.
        /// </summary>
        public IEnumerable<SqlTokenTerminal> SeparatorTokens { get { return ItemsWithoutParenthesis.Where( ( x, i ) => i % 2 != 0 ).Cast<SqlTokenTerminal>(); } }
        
        public SqlTokenIdentifier IdentifierAt( int index )
        {
            return (SqlTokenIdentifier)Slots[index * 2 + 1];
        }

        public int IdentifiersCount
        {
            get { return (Slots.Length - 1) / 2; }
        }

        public IEnumerable<SqlTokenIdentifier> Identifiers
        {
            get { return ItemsWithoutParenthesis.Where( ( x, i ) => i % 2 == 0 ).Cast<SqlTokenIdentifier>(); }
        }

        public SqlExprMultiIdentifier RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            ISqlItem[] c = SqlExprBaseListWithSeparator<SqlTokenIdentifier>.ReplaceNonSeparator( Slots, true, t => t.RemoveQuoteIfPossible( keepIfReservedKeyword ) );
            return c != null ? new SqlExprMultiIdentifier( c ) : this;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        bool ISqlIdentifier.IsVariable
        {
            get { return false; }
        }
    }

}
