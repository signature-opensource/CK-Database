using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    public class SqlExprCollate : SqlExpr
    {
        public SqlExprCollate( SqlExpr left, SqlTokenIdentifier collateT, SqlTokenIdentifier nameT )
            : this( CreateArray( SqlToken.EmptyOpenPar, left, collateT, nameT, SqlToken.EmptyClosePar ) )
        {
        }

        internal SqlExprCollate( ISqlItem[] items )
            : base( items )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
