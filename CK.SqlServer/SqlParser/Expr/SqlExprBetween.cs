using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
  
    /// <summary>
    /// 
    /// </summary>
    public class SqlExprBetween : SqlExpr
    {
        public SqlExprBetween( SqlExpr left, SqlTokenIdentifier notToken, SqlTokenIdentifier betweenToken, SqlExpr start, SqlTokenIdentifier andToken, SqlItem stop )
            : this( Build( left, notToken, betweenToken, start, andToken, stop ) )
        {
        }

        internal SqlExprBetween( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenIdentifier notToken, SqlTokenIdentifier betweenToken, SqlExpr start, SqlTokenIdentifier andToken, SqlItem stop )
        {
            return notToken != null
                            ? CreateArray( SqlToken.EmptyOpenPar, left, notToken, betweenToken, start, andToken, stop, SqlToken.EmptyClosePar )
                            : CreateArray( SqlToken.EmptyOpenPar, left, betweenToken, start, andToken, stop, SqlToken.EmptyClosePar );
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public bool IsNotBetween { get { return Slots.Length == 8; } }

        public SqlTokenIdentifier NotToken { get { return IsNotBetween ? (SqlTokenIdentifier)Slots[2] : null; } }

        public SqlTokenIdentifier BetweenToken { get { return (SqlTokenIdentifier)Slots[IsNotBetween ? 3 : 2]; } }

        public SqlExpr Start { get { return (SqlExpr)Slots[IsNotBetween ? 4 : 3]; } }

        public SqlTokenIdentifier AndToken { get { return (SqlTokenIdentifier)Slots[IsNotBetween ? 5 : 4]; } }

        public SqlExpr Stop { get { return (SqlExpr)Slots[IsNotBetween ? 6 : 5]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
