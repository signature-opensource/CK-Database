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
    public class SqlExprIsNull : SqlExpr
    {
        public SqlExprIsNull( SqlItem left, SqlTokenIdentifier isTok, SqlTokenIdentifier notTok, SqlTokenIdentifier nullTok )
            : this( Build( left, isTok, notTok, nullTok ) )
        {
        }

        static ISqlItem[] Build( SqlItem left, SqlTokenIdentifier isTok, SqlTokenIdentifier notTok, SqlTokenIdentifier nullTok )
        {
            return notTok != null 
                        ? CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isTok, notTok, nullTok, SqlExprMultiToken<SqlTokenClosePar>.Empty )
                        : CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isTok, nullTok, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        internal SqlExprIsNull( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlItem Left { get { return (SqlItem)Slots[1]; } }

        public SqlTokenIdentifier IsTok { get { return (SqlTokenIdentifier)Slots[2]; } }

        public bool IsNotNull { get { return Slots.Length == 6; } }

        public SqlTokenIdentifier NotTok { get { return IsNotNull ? (SqlTokenIdentifier)Slots[3] : null; } }

        public SqlTokenIdentifier NullTok { get { return (SqlTokenIdentifier)Slots[IsNotNull ? 4 : 3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }


    }


}
