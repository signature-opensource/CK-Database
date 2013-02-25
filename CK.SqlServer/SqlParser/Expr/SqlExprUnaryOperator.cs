using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprUnaryOperator : SqlExpr
    {
        readonly ReadOnlyListMono<SqlTokenTerminal> _firstToken;
        
        public SqlExprUnaryOperator( SqlTokenTerminal op, SqlExpr rightExpr )
        {
            if( op == null ) throw new ArgumentNullException( "op" );
            if( rightExpr == null ) throw new ArgumentNullException( "rightExpr" );
            _firstToken = new ReadOnlyListMono<SqlTokenTerminal>( op );
            Expression = rightExpr;
        }

        public SqlTokenTerminal Operator { get { return _firstToken[0]; } }

        public SqlExpr Expression { get; private set; }

        public override IEnumerable<SqlToken> Tokens
        {
            get { return _firstToken.Concat( Expression.Tokens ); }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
