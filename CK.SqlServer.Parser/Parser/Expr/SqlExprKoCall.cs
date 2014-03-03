using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlExprKoCall : SqlExpr
    {
        public SqlExprKoCall( SqlItem funName, SqlExprCommaList parameters, SqlNoExprOverClause over )
            : this( Build( funName, parameters, over ) )
        {
        }

        static ISqlItem[] Build( SqlItem funName, SqlExprCommaList parameters, SqlNoExprOverClause over )
        {
            if( funName == null ) throw new ArgumentNullException( "targetName" );
            if( parameters == null ) throw new ArgumentNullException( "parameters" );
            return  over != null 
                    ? CreateArray( SqlToken.EmptyOpenPar, funName, parameters, over, SqlToken.EmptyClosePar )
                    : CreateArray( SqlToken.EmptyOpenPar, funName, parameters, SqlToken.EmptyClosePar );
        }

        internal SqlExprKoCall( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlItem FunName { get { return (SqlItem)Slots[1]; } }

        public SqlExprCommaList Parameters { get { return (SqlExprCommaList)Slots[2]; } }

        public SqlNoExprOverClause OverClause { get { return Slots.Length == 5 ? (SqlNoExprOverClause)Slots[3] : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
