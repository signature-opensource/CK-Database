using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprParameterDefaultValue : SqlExpr
    {
        readonly SqlToken[] _tokens;

        public SqlExprParameterDefaultValue( SqlTokenTerminal assignToken, SqlTokenTerminal minusSign, SqlTokenBaseLiteral value )
        {
            if( assignToken == null ) throw new ArgumentNullException( "assignToken" );
            if( minusSign != null && minusSign.TokenType == SqlTokenType.Minus ) throw new ArgumentException( "Must be null or minus." );
            if( value == null ) throw new ArgumentNullException( "value" );

            _tokens = minusSign == null ? CreateArray( assignToken, value ) : CreateArray( assignToken, minusSign, value );
        }

        public SqlExprParameterDefaultValue( SqlTokenTerminal assignToken, SqlTokenIdentifier variable )
        {
            if( assignToken == null ) throw new ArgumentNullException( "assignToken" );
            if( variable == null ) throw new ArgumentNullException( "variable" );

            _tokens = CreateArray( assignToken, variable );
        }

        public bool IsVariable { get { return _tokens.Length == 2 && _tokens[1].TokenType == SqlTokenType.IdentifierVariable; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _tokens; } }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
