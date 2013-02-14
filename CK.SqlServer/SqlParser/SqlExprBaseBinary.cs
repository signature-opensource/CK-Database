using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseBinary : SqlExpr
    {
        readonly ReadOnlyListMono<SqlToken> _middleToken;

        protected SqlExprBaseBinary( SqlExpr left, SqlToken middle, SqlExpr right )
        {
            if( left == null ) throw new ArgumentNullException( "left" );
            if( middle == null ) throw new ArgumentNullException( "middle" );
            if( right == null ) throw new ArgumentNullException( "right" );
            Left = left;
            _middleToken = new ReadOnlyListMono<SqlToken>( middle );
            Right = right;
        }

        public SqlExpr Left { get; private set; }

        protected SqlToken Middle { get { return _middleToken[0]; } }

        public SqlExpr Right { get; private set; }

        public override IEnumerable<SqlToken> Tokens
        {
            get { return Left.Tokens.Concat( _middleToken.Concat( Right.Tokens ) ); }
        }

    }

}
