using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlExprParameterDefaultValue : SqlItem
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

        public bool IsNull { get { return _tokens.Length == 2 && _tokens[1].TokenType == SqlTokenType.Null; } }
        
        public bool IsLiteral { get { return _tokens.Length == 3 || (_tokens[1].TokenType & SqlTokenType.LitteralMask) != 0; } }

        public sealed override IEnumerable<ISqlItem> Items { get { return _tokens; } }

        public override SqlToken FirstOrEmptyToken { get { return _tokens[0]; } }

        public override SqlToken LastOrEmptyToken { get { return _tokens[_tokens.Length - 1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
