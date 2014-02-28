using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlExprUnaryOperator : SqlExpr
    {
        public SqlExprUnaryOperator( SqlToken op, SqlExpr rightExpr )
            : this( Build( op, rightExpr ) )
        {
        }

        static ISqlItem[] Build( SqlToken op, SqlExpr rightExpr )
        {
            if( op == null ) throw new ArgumentNullException( "op" );
            if( rightExpr == null ) throw new ArgumentNullException( "rightExpr" );
            return CreateArray( SqlToken.EmptyOpenPar, op, rightExpr, SqlToken.EmptyClosePar );
        }

        internal SqlExprUnaryOperator( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlToken Operator { get { return (SqlToken)Slots[1]; } }

        public SqlExpr Expression { get { return (SqlExpr)Slots[2]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
