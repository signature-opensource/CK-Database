using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprSyntaxError : SqlExprBaseMonoToken
    {
        public SqlExprSyntaxError( string message )
            : base( new SqlTokenError( message ) )
        {
        }

        public SqlExprSyntaxError( SqlTokenError error )
            : base( error )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
