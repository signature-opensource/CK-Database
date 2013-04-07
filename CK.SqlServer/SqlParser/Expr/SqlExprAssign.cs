using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprAssign : SqlExprBaseBinary
    {
        public SqlExprAssign( ISqlIdentifier identifier, SqlTokenTerminal assignToken, SqlExpr right )
            : base( (SqlExpr)identifier, assignToken, right )
        {
            if( (assignToken.TokenType & SqlTokenType.IsAssignOperator) == 0 ) throw new ArgumentException( "Invalid assign token.", "assignToken" );
        }

        internal SqlExprAssign( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
        }

        public new ISqlIdentifier Left { get { return (ISqlIdentifier)base.Left; } }

        public SqlTokenTerminal AssignToken { get { return (SqlTokenTerminal)Middle; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
