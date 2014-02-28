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
        public SqlExprIsNull( SqlItem left, SqlTokenIdentifier isToken, SqlTokenIdentifier notToken, SqlTokenIdentifier nullToken )
            : this( Build( left, isToken, notToken, nullToken ) )
        {
        }

        static ISqlItem[] Build( SqlItem left, SqlTokenIdentifier isToken, SqlTokenIdentifier notToken, SqlTokenIdentifier nullToken )
        {
            return notToken != null 
                        ? CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isToken, notToken, nullToken, SqlExprMultiToken<SqlTokenClosePar>.Empty )
                        : CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Empty, left, isToken, nullToken, SqlExprMultiToken<SqlTokenClosePar>.Empty );
        }

        internal SqlExprIsNull( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlItem Left { get { return (SqlItem)Slots[1]; } }

        public SqlTokenIdentifier IsToken { get { return (SqlTokenIdentifier)Slots[2]; } }

        public bool IsNotNull { get { return Slots.Length == 6; } }

        public SqlTokenIdentifier NotToken { get { return IsNotNull ? (SqlTokenIdentifier)Slots[3] : null; } }

        public SqlTokenIdentifier NullToken { get { return (SqlTokenIdentifier)Slots[IsNotNull ? 4 : 3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }


    }


}
