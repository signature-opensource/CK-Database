using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprTypeDecimal : SqlExpr, ISqlExprUnifiedType
    {
        readonly SqlToken[] _tokens;

        public SqlExprTypeDecimal( IEnumerable<SqlToken> tokens, byte precision = 18, byte scale = 0 )
        {
            if( tokens == null ) throw new ArgumentNullException( "tokens" );
            _tokens = tokens.ToArray();
            if( _tokens.Length != 1 && _tokens.Length != 4 && _tokens.Length != 6 ) throw new ArgumentException( "Expected decimal|numeric[(precision[,scale=0])]: 1, 4 or 6 tokens." );
            if( _tokens.Length == 1 && precision != 18 ) throw new ArgumentException( "Precision must default to 18.", "precision" );
            if( _tokens.Length != 6 && scale != 0 ) throw new ArgumentException( "Scale must be 0.", "scale" );
            if( !(_tokens[0] is SqlTokenIdentifier) 
                || TypeIdentifier.TokenType != SqlTokenType.IdentifierTypeDecimal )
            {
                throw new ArgumentException( "Invalid decimal token.", "tokens" );
            }
            if( precision > 38 ) throw new ArgumentException( "Precision is at most 38.", "precision" );
            if( precision < scale ) throw new ArgumentException( "Scale can not be greater than Precision." );
            SyntaxPrecision = precision;
            SyntaxScale = scale;
        }

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

        int ISqlExprUnifiedType.SyntaxSize
        {
            get { return -2; }
        }

        int ISqlExprUnifiedType.SyntaxSecondScale
        {
            get { return -1; }
        }
    }

}
