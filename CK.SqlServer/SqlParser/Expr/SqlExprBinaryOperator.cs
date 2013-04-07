using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprBinaryOperator : SqlExprBaseBinary
    {
        public SqlExprBinaryOperator( SqlExpr left, SqlTokenTerminal op, SqlExpr right )
            : base( left, op, right )
        {
            if( !IsValidOperator( op.TokenType ) ) throw new ArgumentException();
        }

        internal SqlExprBinaryOperator( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
            Debug.Assert( IsValidOperator( Middle.TokenType ) );
        }

        static public bool IsValidOperator( SqlTokenType op )
        {
            if( op > 0 )
            {
                if( (op & SqlTokenType.IsCompareOperator) != 0 ) return true;
                if( (op & SqlTokenType.IsBasicOperator) != 0 )
                {
                    if( op != SqlTokenType.BitwiseNot && op != SqlTokenType.Is) return true;
                }
                else if( op == SqlTokenType.And || op == SqlTokenType.Or ) return true;
            }
            return false;
        }

        public new SqlTokenTerminal Middle { get { return (SqlTokenTerminal)base.Middle; } }

        public SqlTokenTerminal Operator { get { return (SqlTokenTerminal)base.Middle; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
