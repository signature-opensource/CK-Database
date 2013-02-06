using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlSyntaxErrorExpr : SqlExpr
    {
        public SqlSyntaxErrorExpr( SourceLocation location, string errorMessageFormat, params object[] messageParameters )
            : base( location )
        {
            ErrorMessage = String.Format( errorMessageFormat, messageParameters );
        }

        public string ErrorMessage { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            return "Syntax: " + ErrorMessage;
        }
    }

}
