using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
  
    /// <summary>
    /// Mono identifier (wraps one <see cref="SqlTokenIdentifier"/>).
    /// </summary>
    public class SqlExprIdentifier : SqlExprBaseMonoToken
    {
        public SqlExprIdentifier( SqlTokenIdentifier t )
            : base( t )
        {
        }

        public new SqlTokenIdentifier Token { get { return (SqlTokenIdentifier)base.Token; } }

        public string Name { get { return Token.Name; } }

        public bool IsVariable { get { return Token.IsVariable; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
