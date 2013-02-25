using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseMonoToken : SqlExpr
    {
        readonly ReadOnlyListMono<SqlToken> _token;

        protected SqlExprBaseMonoToken( SqlToken t )
        {
            _token = new ReadOnlyListMono<SqlToken>( t );
        }

        public SqlToken Token { get { return _token[0]; } }

        public override IEnumerable<SqlToken> Tokens { get { return _token; } }
    }


}
