using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlCommentExpr : SqlExpr
    {
        public SqlCommentExpr( SourceLocation location, SqlToken t, string comment )
            : base( location )
        {
            if( comment == null ) throw new ArgumentNullException( "comment" );
            if( t < 0 && (t & SqlToken.IsComment) == 0 ) throw new ArgumentException( "Expected SqlToken.IsComment." );
            IsLineComment = (t == SqlToken.LineComment);
        }

        public string Comment { get; private set; }

        public bool IsLineComment { get; private set; }

        public SqlLiteralExpr DefaultValue { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            if( IsLineComment ) return "-- " + Comment + Environment.NewLine;
            return "/*" + Comment + "*/";
        }
    }

}
