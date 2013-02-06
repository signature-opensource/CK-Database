using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public abstract class SqlBinaryExpr : SqlExpr
    {
        protected SqlBinaryExpr( SourceLocation location, SqlExpr left, SqlExpr right )
            : base( location )
        {
            if( left == null ) throw new ArgumentNullException( "left" );
            if( right == null ) throw new ArgumentNullException( "right" );
            Left = left;
            Right = right;
        }

        public SqlExpr Left { get; private set; }

        public SqlExpr Right { get; private set; }
    }

}
