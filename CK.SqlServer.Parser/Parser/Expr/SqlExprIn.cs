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
    public class SqlExprIn : SqlExpr
    {
        public SqlExprIn( SqlExpr left, SqlTokenIdentifier notTok, SqlTokenIdentifier inTok, SqlExprCommaList values )
            : this( Build( left, notTok, inTok, values ) )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenIdentifier notTok, SqlTokenIdentifier inTok, SqlExprCommaList values )
        {
            return notTok != null
                            ? CreateArray( SqlToken.EmptyOpenPar, left, notTok, inTok, values, SqlToken.EmptyClosePar )
                            : CreateArray( SqlToken.EmptyOpenPar, left, inTok, values, SqlToken.EmptyClosePar );
        }

        internal SqlExprIn( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public bool IsNotIn { get { return Slots.Length == 6; } }

        public SqlTokenIdentifier NotTok { get { return IsNotIn ? (SqlTokenIdentifier)Slots[2] : null; } }

        public SqlTokenIdentifier InTok { get { return (SqlTokenIdentifier)Slots[IsNotIn ? 3 : 2]; } }

        public SqlExprCommaList Values { get { return (SqlExprCommaList)Slots[IsNotIn ? 4 : 3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
