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
        public SqlExprBetween( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal betweenToken, SqlExpr  start, SqlTokenTerminal andToken, SqlItem stop )
            : this( Build( left, notToken, betweenToken, start, andToken, stop ) )
        {
        }

        internal SqlExprBetween( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal betweenToken, SqlExpr start, SqlTokenTerminal andToken, SqlItem stop )
        {
            return notToken != null
                            ? CreateArray( SqlToken.EmptyOpenPar, left, notToken, betweenToken, start, andToken, stop, SqlToken.EmptyClosePar )
                            : CreateArray( SqlToken.EmptyOpenPar, left, betweenToken, start, andToken, stop, SqlToken.EmptyClosePar );
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public bool IsNotBetween { get { return Slots.Length == 8; } }

        public SqlTokenTerminal NotToken { get { return IsNotBetween ? (SqlTokenTerminal)Slots[2] : null; } }

        public SqlTokenTerminal BetweenToken { get { return (SqlTokenTerminal)Slots[IsNotBetween ? 3 : 2]; } }

        public SqlExpr Start { get { return (SqlExpr)Slots[IsNotBetween ? 4 : 3]; } }

        public SqlTokenTerminal AndToken { get { return (SqlTokenTerminal)Slots[IsNotBetween ? 5 : 4]; } }

        public SqlExpr Stop { get { return (SqlExpr)Slots[IsNotBetween ? 6 : 5]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
