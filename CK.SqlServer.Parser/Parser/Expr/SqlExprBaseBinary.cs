using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public abstract class SqlExprBaseBinary : SqlExpr
    {
        protected SqlExprBaseBinary( SqlExpr left, ISqlItem middle, SqlExpr right )
            : this( Build( left, middle, right ) )
        {
        }

        static ISqlItem[] Build( SqlItem left, ISqlItem middle, SqlItem right )
        {
            if( left == null ) throw new ArgumentNullException( "left" );
            if( middle == null ) throw new ArgumentNullException( "middle" );
            if( right == null ) throw new ArgumentNullException( "right" );
            return CreateArray( SqlToken.EmptyOpenPar, left, middle, right, SqlToken.EmptyClosePar );
        }

        protected SqlExprBaseBinary( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public SqlExpr Left { get { return (SqlExpr)Slots[1]; } }

        protected ISqlItem Middle { get { return Slots[2]; } }

        public SqlExpr Right { get { return (SqlExpr)Slots[3]; } }

    }

}
