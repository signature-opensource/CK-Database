using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public abstract class SqlExprBaseMonoToken<T> : SqlExpr 
        where T : SqlToken 
    {
        readonly T[] _token;

        protected SqlExprBaseMonoToken( T t )
        {
            _token = new[]{ t };
        }

        public T Token { get { return _token[0]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _token; } }

        public override IEnumerable<SqlToken> Tokens { get { return _token; } }

        protected IEnumerable<T> TypedTokens { get { return _token; } }
    }


}
