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

        static ISqlItem[] Build( SqlTokenIdentifier execT, SqlTokenIdentifier asT, SqlToken rightT )
        {
            if( execT == null || execT.TokenType != SqlTokenType.Execute ) throw new ArgumentException( "execT" );
            if( asT == null || asT.TokenType != SqlTokenType.As ) throw new ArgumentException( "asT" );
            if( rightT == null ) throw new ArgumentNullException( "rightT" );
            return new ISqlItem[]{ execT, asT, rightT };
        }

        internal SqlNoExprExecuteAs( ISqlItem[] newItems )
            : base( newItems )
        {
        }

        public SqlTokenIdentifier ExecT { get { return (SqlTokenIdentifier)Slots[0]; } }

        protected SqlTokenIdentifier AsT { get { return (SqlTokenIdentifier)Slots[1]; } }

        public SqlToken RightT { get { return (SqlToken)Slots[2]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
