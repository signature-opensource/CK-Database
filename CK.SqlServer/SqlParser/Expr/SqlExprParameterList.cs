using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprParameterList : SqlExprBaseListWithSeparatorList<SqlExprParameter>
    {
        readonly SqlTokenTerminal _openPar;
        readonly SqlTokenTerminal _closePar;

        /// <summary>
        /// Initializes a new list of parameters with optional enclosing parentheses.
        /// </summary>
        /// <param name="openPar">Can be null.</param>
        /// <param name="tokens">Comma separated list of <see cref="SqlExprParameter"/> (possibly empty).</param>
        /// <param name="closePar">Null if and only if <paramref name="openPar"/> is null.</param>
        public SqlExprParameterList( SqlTokenTerminal openPar, IEnumerable<IAbstractExpr> tokens, SqlTokenTerminal closePar )
            : base( tokens, true )
        {
            if( (_openPar == null) != (_closePar == null) 
                || (_openPar != null && (_openPar.TokenType != SqlTokenType.OpenPar || _closePar.TokenType != SqlTokenType.ClosePar ) ) ) throw new ArgumentException( "Open/Close parenthesis incoherency." ); 
            _openPar = openPar;
            _closePar = closePar;
        }

        internal SqlExprParameterList( SqlTokenTerminal openPar, SqlTokenTerminal closePar, IAbstractExpr[] tokens )
            : base( tokens )
        {
            Debug.Assert( tokens != null );
            DebugCheckArray( tokens, true, IsCommaSeparator );
            Debug.Assert( (_openPar == null) == (_closePar == null) 
                            && (_openPar == null || (_openPar.TokenType == SqlTokenType.OpenPar && _closePar.TokenType == SqlTokenType.ClosePar ) ) );
            _openPar = openPar;
            _closePar = closePar;
        }

        static internal string BuildArray( IEnumerator<IAbstractExpr> tokens, out IAbstractExpr[] result )
        {
            return SqlExprBaseListWithSeparator<SqlExprParameter>.BuildArray( tokens, true, e => e is SqlExprParameter, "parameter", out result );
        }

        /// <summary>
        /// Gets whether parenthesis enclose the parameters.
        /// </summary>
        public bool HasEnclosingParenthesis 
        { 
            get { return _openPar != null; } 
        }

        /// <summary>
        /// Gets the tokens.
        /// </summary>
        public override IEnumerable<SqlToken> Tokens
        {
            get
            {
                if( _openPar != null ) return new ReadOnlyListMono<SqlToken>( _openPar ).Concat( base.Tokens ).Concat( new ReadOnlyListMono<SqlToken>( _closePar ) );
                return base.Tokens;
            }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
