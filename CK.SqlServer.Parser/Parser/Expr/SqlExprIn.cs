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
        public SqlExprIn( SqlExpr left, SqlTokenIdentifier notToken, SqlTokenIdentifier inToken, SqlExprCommaList values )
            : this( Build( left, notToken, inToken, values ) )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenIdentifier notToken, SqlTokenIdentifier inToken, SqlExprCommaList values )
        {
            return notToken != null
                            ? CreateArray( SqlToken.EmptyOpenPar, left, notToken, inToken, values, SqlToken.EmptyClosePar )
                            : CreateArray( SqlToken.EmptyOpenPar, left, inToken, values, SqlToken.EmptyClosePar );
        }

        internal SqlExprIn( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        public bool IsNotIn { get { return Slots.Length == 6; } }

        public SqlTokenIdentifier NotToken { get { return IsNotIn ? (SqlTokenIdentifier)Slots[2] : null; } }

        public SqlTokenIdentifier InToken { get { return (SqlTokenIdentifier)Slots[IsNotIn ? 3 : 2]; } }

        public SqlExprCommaList Values { get { return (SqlExprCommaList)Slots[IsNotIn ? 4 : 3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
