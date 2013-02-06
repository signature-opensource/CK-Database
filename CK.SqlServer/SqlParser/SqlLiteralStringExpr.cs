using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlLiteralStringExpr : SqlLiteralExpr
    {
        public SqlLiteralStringExpr( SourceLocation location, SqlToken t, string value )
            : base( location, t )
        {
            if( value == null ) throw new ArgumentNullException( "value" );
            Value = value;
        }

        public bool IsUnicode { get { return SqlToken == SqlServer.SqlToken.UnicodeString; } }

        public string Value { get; private set; }

        public override string LiteralValue { get { return String.Format( IsUnicode ? "N'{0}'" : "'{0}'", SqlHelper.SqlEncode( Value ) ); } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
