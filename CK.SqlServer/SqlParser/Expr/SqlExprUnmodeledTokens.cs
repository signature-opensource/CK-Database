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
    public class SqlExprUnmodeledTokens : SqlItem
    {
        readonly SqlToken[] _tokens;

        public SqlExprUnmodeledTokens( IEnumerable<SqlToken> tokens )
        {
            if( tokens == null ) throw new ArgumentNullException( "tokens" );
            _tokens = tokens.ToArray();
            if( _tokens.Length == 0 ) throw new ArgumentException( "tokens" );
        }

        public override IEnumerable<ISqlItem> Components { get { return _tokens; } }

        public override IEnumerable<SqlToken> Tokens { get { return _tokens; } }

        public override SqlToken FirstOrEmptyToken { get { return _tokens[0]; } }
        
        public override SqlToken LastOrEmptyToken { get { return _tokens[_tokens.Length-1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
