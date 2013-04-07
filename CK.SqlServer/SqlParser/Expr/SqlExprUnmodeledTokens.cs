using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
using System.Diagnostics;
using System.Globalization;

namespace CK.SqlServer
{
    public class SqlExprUnmodeledTokens : SqlExpr
    {
        readonly SqlToken[] _tokens;

        public SqlExprUnmodeledTokens( IEnumerable<SqlToken> tokens )
        {
            if( tokens == null ) throw new ArgumentNullException( "tokens" );
            _tokens = tokens.ToArray();
        }

        public override IEnumerable<IAbstractExpr> Components { get { return _tokens; } }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
