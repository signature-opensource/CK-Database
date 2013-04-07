using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{

    public abstract class SqlExprBaseMultiIdentifier : SqlExpr, IReadOnlyList<SqlTokenIdentifier>
    {
        readonly IAbstractExpr[] _components;
        
        /// <summary>
        /// Initializes a new <see cref="SqlExprBaseMultiIdentifier"/> whith a <paramref name="validSeparator"/> that defaults to <see cref="IsDotOrDoubleColonSeparator"/>.
        /// </summary>
        /// <param name="tokens">Identifier an separator tokens.</param>
        /// <param name="validSeparator">Separator validator.</param>
        public SqlExprBaseMultiIdentifier( IList<IAbstractExpr> tokens, Predicate<IAbstractExpr> validSeparator = null )
        {
            _components = tokens.ToArray();
            SqlExprBaseListWithSeparator<SqlTokenIdentifier>.CheckArray( _components, false, false, false, validSeparator ?? IsDotOrDoubleColonSeparator );
        }

        internal SqlExprBaseMultiIdentifier( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        static internal string BuildArray( IEnumerator<IAbstractExpr> tokens, out IAbstractExpr[] result )
        {
            return SqlExprBaseListWithSeparator<SqlTokenIdentifier>.BuildArray( tokens, false, IsDotOrDoubleColonSeparator, "identifier", out result );
        }

        /// <summary>
        /// Gets the number of <see cref="SeparatorTokens"/>.
        /// </summary>
        public int SeparatorCount { get { return _components.Length / 2; } }

        /// <summary>
        /// Gets the separators token.
        /// </summary>
        public IEnumerable<SqlTokenTerminal> SeparatorTokens { get { return _components.Where( ( x, i ) => i % 2 != 0 ).Cast<SqlTokenTerminal>(); } }

        public SqlTokenIdentifier this[int index]
        {
            get { return (SqlTokenIdentifier)_components[index * 2]; }
        }

        /// <summary>
        /// Gets the number of <see cref="SqlTokenIdentifier"/>.
        /// </summary>
        public int Count
        {
            get { return (_components.Length + 1) / 2; }
        }

        /// <summary>
        /// Gets the identifiers.
        /// </summary>
        public IEnumerator<SqlTokenIdentifier> GetEnumerator()
        {
            return _components.Where( ( x, i ) => i % 2 == 0 ).Cast<SqlTokenIdentifier>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override IEnumerable<IAbstractExpr> Components
        {
            get { return _components; }
        }

        protected IAbstractExpr[] RemoveQuoteIfPossible( bool keepIfReservedKeyword )
        {
            return SqlExprBaseListWithSeparator<SqlTokenIdentifier>.ReplaceNonSeparator( _components, false, t => t.RemoveQuoteIfPossible( keepIfReservedKeyword ) );
        }

    }

}
