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
    public class SqlExprIn : SqlExpr
    {
        public SqlExprIn( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal inToken, SqlExprCommaList values )
            : this( Build( left, notToken, inToken, values ) )
        {
        }

        static ISqlItem[] Build( SqlExpr left, SqlTokenTerminal notToken, SqlTokenTerminal inToken, SqlExprCommaList values )
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

        public SqlTokenTerminal NotToken { get { return IsNotIn ? (SqlTokenTerminal)Slots[2] : null; } }

        public SqlTokenTerminal InToken { get { return (SqlTokenTerminal)Slots[IsNotIn ? 3 : 2]; } }

        public SqlExprCommaList Values { get { return (SqlExprCommaList)Slots[IsNotIn ? 4 : 3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
