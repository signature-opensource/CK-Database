using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public abstract class SqlLiteralExpr : SqlExpr
    {
        public SqlLiteralExpr( SourceLocation location, SqlToken t )
            : base( location )
        {
            if( t < 0 || (t & (SqlToken.IsString | SqlToken.IsNumber | SqlToken.Identifier)) == 0 ) throw new ArgumentException( "Invalid literal token.", "t" );
            SqlToken = t;
        }

        public SqlToken SqlToken { get; private set; }

        public abstract string LiteralValue { get; }

        public override string ToString()
        {
            return LiteralValue;
        }
    }

}
