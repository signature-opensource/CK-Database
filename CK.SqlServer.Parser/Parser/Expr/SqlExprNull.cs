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
        public SqlExprNull( SqlTokenIdentifier tokenIdentifier )
            : base( tokenIdentifier )
        {
            if( String.Compare( tokenIdentifier.Name, "null", StringComparison.OrdinalIgnoreCase ) != 0 ) throw new ArgumentException( "Invalid null token.", "tokenIdentifier" );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
