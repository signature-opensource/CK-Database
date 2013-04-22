using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprKoCall : SqlExpr
    {
        public SqlExprKoCall( SqlItem funName, SqlExprCommaList parameters )
            : this( Build( funName, parameters ) )
        {
        }

        static ISqlItem[] Build( SqlItem funName, SqlExprCommaList parameters )
        {
            if( funName == null ) throw new ArgumentNullException( "targetName" );
            if( parameters == null ) throw new ArgumentNullException( "parameters" );
            return CreateArray( SqlToken.EmptyOpenPar, funName, parameters, SqlToken.EmptyClosePar );
        }

        internal SqlExprKoCall( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlItem FunName { get { return (SqlItem)Slots[1]; } }

        public SqlExprCommaList Parameters { get { return (SqlExprCommaList)Slots[2]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
