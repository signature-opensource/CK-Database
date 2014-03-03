using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
  
    /// <summary>
    /// 
    /// </summary>
    public class SqlExprBetween : SqlExpr
    {
        public SqlExprBetween( SqlExpr left, SqlTokenIdentifier notTok, SqlTokenIdentifier betweenTok, SqlExpr start, SqlTokenIdentifier andTok, SqlItem stop )
            : this( Build( left, notTok, betweenTok, start, andTok, stop ) )
        {
        }

        internal SqlExprBetween( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenIdentifier notTok, SqlTokenIdentifier betweenTok, SqlExpr start, SqlTokenIdentifier andTok, SqlItem stop )
        {
            return notTok != null
                            ? CreateArray( SqlToken.EmptyOpenPar, left, notTok, betweenTok, start, andTok, stop, SqlToken.EmptyClosePar )
                            : CreateArray( SqlToken.EmptyOpenPar, left, betweenTok, start, andTok, stop, SqlToken.EmptyClosePar );
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public bool IsNotBetween { get { return Slots.Length == 8; } }

        public SqlTokenIdentifier NotTok { get { return IsNotBetween ? (SqlTokenIdentifier)Slots[2] : null; } }

        public SqlTokenIdentifier BetweenTok { get { return (SqlTokenIdentifier)Slots[IsNotBetween ? 3 : 2]; } }

        public SqlExpr Start { get { return (SqlExpr)Slots[IsNotBetween ? 4 : 3]; } }

        public SqlTokenIdentifier AndTok { get { return (SqlTokenIdentifier)Slots[IsNotBetween ? 5 : 4]; } }

        public SqlExpr Stop { get { return (SqlExpr)Slots[IsNotBetween ? 6 : 5]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
