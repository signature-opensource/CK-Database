#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprParameterDefaultValue.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

        public bool HasMinusSign { get { return _tokens.Length == 3; } }

        /// <summary>
        /// Gets the default value (<see cref="IsVariable"/> must be false).
        /// It can be <see cref="DBNull.Value"/>, a <see cref="Int32"/>, a <see cref="Double"/> or a string for numerics and money:
        /// .Net <see cref="Decimal"/> type has only 28 digits whereas Sql server numerics has 38. And money is actually a Int64 for
        /// sql server.
        /// </summary>
        public object NullOrLitteralDotNetValue
        {
            get
            {
                if( IsVariable ) throw new InvalidOperationException();
                if( IsNull ) return DBNull.Value;
                Debug.Assert( IsLiteral );
                SqlTokenBaseLiteral t = (SqlTokenBaseLiteral)_tokens[_tokens.Length == 3 ? 2 : 1];
                if( (t.TokenType & SqlTokenType.IsString) != 0 )
                {
                    return ((SqlTokenLiteralString)t).Value;
                }
                Debug.Assert( (t.TokenType & SqlTokenType.IsNumber) != 0 );
                if( t.TokenType == SqlTokenType.Integer )
                {
                    int v = ((SqlTokenLiteralInteger)t).Value;
                    return HasMinusSign ? -v : v;
                }
                if( t.TokenType == SqlTokenType.Decimal )
                {
                    string s = ((SqlTokenLiteralDecimal)t).Value;
                    return HasMinusSign ? '-' + s : s;
                }
                if( t.TokenType == SqlTokenType.Float )
                {
                    double d = ((SqlTokenLiteralFloat)t).Value;
                    return HasMinusSign ? -d : d;
                }
                if( t.TokenType == SqlTokenType.Money )
                {
                    string s = ((SqlTokenLiteralMoney)t).Value;
                    return HasMinusSign ? '-' + s : s;
                }
                throw new NotSupportedException();
            }
        }

        public sealed override IEnumerable<ISqlItem> Items { get { return _tokens; } }

        public override SqlToken FirstOrEmptyT { get { return _tokens[0]; } }

        public override SqlToken LastOrEmptyT { get { return _tokens[_tokens.Length - 1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
