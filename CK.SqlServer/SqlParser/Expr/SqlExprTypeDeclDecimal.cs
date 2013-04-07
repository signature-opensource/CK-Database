using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprTypeDeclDecimal : SqlExpr, ISqlExprUnifiedTypeDecl
    {
        readonly SqlToken[] _tokens;

        public SqlExprTypeDeclDecimal( SqlTokenIdentifier id )
        {
            if( id == null ) throw new ArgumentNullException( "id" );
            if( id.TokenType != SqlTokenType.IdentifierTypeDecimal )
            {
                throw new ArgumentException( "Invalid decimal token.", "id" );
            }
            _tokens = CreateArray( id );
            SyntaxPrecision = 0;
            SyntaxScale = 0;
        }

        public SqlExprTypeDeclDecimal( SqlTokenIdentifier id, SqlTokenTerminal openPar, SqlTokenLiteralInteger precision, SqlTokenTerminal closePar )
        {
            if( id == null ) throw new ArgumentNullException( "id" );
            if( openPar == null ) throw new ArgumentNullException( "openPar" );
            if( openPar.TokenType != SqlTokenType.OpenPar ) throw new ArgumentException( "Must be '('.", "openPar" );
            if( precision == null ) throw new ArgumentNullException( "secondScale" );
            if( closePar == null ) throw new ArgumentNullException( "closePar" );
            if( closePar.TokenType != SqlTokenType.ClosePar ) throw new ArgumentException( "Must be ')'.", "closePar" );
            if( id.TokenType != SqlTokenType.IdentifierTypeDecimal )
            {
                throw new ArgumentException( "Invalid decimal token.", "id" );
            }
            if( precision.Value <= 0 || precision.Value > 38 )
            {
                throw new ArgumentException( "Invalid precision.", "precision" );
            }

            _tokens = CreateArray( id, openPar, precision, closePar );
            SyntaxPrecision = (byte)precision.Value;
            SyntaxScale = 0;
        }

        public SqlExprTypeDeclDecimal( SqlTokenIdentifier id, SqlTokenTerminal openPar, SqlTokenLiteralInteger precision, SqlTokenTerminal comma, SqlTokenLiteralInteger scale, SqlTokenTerminal closePar )
        {
            if( id == null ) throw new ArgumentNullException( "id" );
            if( openPar == null ) throw new ArgumentNullException( "openPar" );
            if( openPar.TokenType != SqlTokenType.OpenPar ) throw new ArgumentException( "Must be '('.", "openPar" );
            if( precision == null ) throw new ArgumentNullException( "secondScale" );
            if( comma == null ) throw new ArgumentNullException( "comma" );
            if( comma.TokenType != SqlTokenType.Comma ) throw new ArgumentException( "Must be ','.", "comma" );
            if( scale == null ) throw new ArgumentNullException( "scale" );
            if( closePar == null ) throw new ArgumentNullException( "closePar" );
            if( closePar.TokenType != SqlTokenType.ClosePar ) throw new ArgumentException( "Must be ')'.", "closePar" );
            if( id.TokenType != SqlTokenType.IdentifierTypeDecimal )
            {
                throw new ArgumentException( "Invalid decimal token.", "id" );
            }
            if( precision.Value <= 0 || precision.Value > 38 )
            {
                throw new ArgumentException( "Invalid precision.", "precision" );
            }
            if( scale.Value < 0 || scale.Value > precision.Value )
            {
                throw new ArgumentException( "Invalid scale (must be less or equalt to precision).", "scale" );
            }

            _tokens = CreateArray( id, openPar, precision, comma, scale, closePar );
            SyntaxPrecision = (byte)precision.Value;
            SyntaxScale = (byte)scale.Value;
        }

        public override IEnumerable<IAbstractExpr> Components { get { return _tokens; } }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        public SqlTokenIdentifier TypeIdentifier 
        {
            get { return (SqlTokenIdentifier)_tokens[0]; } 
        }

        public SqlDbType DbType { get { return SqlDbType.Decimal; } }

        public byte SyntaxPrecision { get; private set; }

        public byte SyntaxScale { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        int ISqlExprUnifiedTypeDecl.SyntaxSize
        {
            get { return -2; }
        }

        int ISqlExprUnifiedTypeDecl.SyntaxSecondScale
        {
            get { return -1; }
        }
    }

}
