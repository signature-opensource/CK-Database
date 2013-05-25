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
    public class SqlExprUnmodeledTokens : SqlNoExpr
    {
        public SqlExprUnmodeledTokens( IEnumerable<SqlToken> tokens )
            : base( Build( tokens ) )
        {
        }

        static ISqlItem[] Build( IEnumerable<SqlToken> tokens )
        {
            if( tokens == null ) throw new ArgumentNullException( "tokens" );
            ISqlItem[] t = tokens.ToArray();
            if( t.Length == 0 ) throw new ArgumentException( "tokens" );
            return t;
        }

        public override IEnumerable<SqlToken> Tokens { get { return (IEnumerable<SqlToken>)Slots; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
