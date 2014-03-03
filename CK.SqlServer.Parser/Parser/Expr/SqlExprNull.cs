using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    public class SqlExprNull : SqlExprBaseMonoToken<SqlTokenIdentifier>
    {
        public SqlExprNull( SqlTokenIdentifier nullT )
            : base( nullT )
        {
            if( nullT.TokenType != SqlTokenType.Null
                || String.Compare( nullT.Name, "null", StringComparison.OrdinalIgnoreCase ) != 0 )
            {
                throw new ArgumentException( "Invalid null token.", "nullT" );
            }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
