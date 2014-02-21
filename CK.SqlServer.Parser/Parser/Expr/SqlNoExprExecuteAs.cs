using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlNoExprExecuteAs : SqlNoExpr
    {
        public SqlNoExprExecuteAs( SqlTokenIdentifier execToken, SqlTokenIdentifier asToken, SqlToken right )
            : this( Build( execToken, asToken, right ) )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier execToken, SqlTokenIdentifier asToken, SqlToken right )
        {
            if( execToken == null || execToken.TokenType != SqlTokenType.Execute ) throw new ArgumentException( "execToken" );
            if( asToken == null || asToken.TokenType != SqlTokenType.As ) throw new ArgumentException( "asToken" );
            if( right == null ) throw new ArgumentNullException( "right" );
            return new ISqlItem[]{ execToken, asToken, right };
        }

        internal SqlNoExprExecuteAs( ISqlItem[] newItems )
            : base( newItems )
        {
        }

        public SqlTokenIdentifier ExecToken { get { return (SqlTokenIdentifier)Slots[0]; } }

        protected SqlTokenIdentifier AsToken { get { return (SqlTokenIdentifier)Slots[1]; } }

        public SqlToken Right { get { return (SqlToken)Slots[2]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
