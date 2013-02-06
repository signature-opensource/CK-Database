using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlTypedIdentifierExpr : SqlExpr
    {
        public SqlTypedIdentifierExpr( SourceLocation location, SqlIdentifierExpr identifier, SqlTypeExpr sqlType )
            : base( location )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( sqlType == null ) throw new ArgumentNullException( "sqlType" );

            Identifier = identifier;
            SqlType = sqlType;
        }

        public SqlIdentifierExpr Identifier { get; private set; }

        public SqlTypeExpr SqlType { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            return String.Format( "{0} {1}", Identifier, SqlType );
        }
    }

}
