using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
  
    /// <summary>
    /// Mono identifier (wraps one <see cref="SqlTokenIdentifier"/>).
    /// </summary>
    public class SqlExprIdentifier : SqlExprBaseMonoToken<SqlTokenIdentifier>, ISqlIdentifier
    {
        public SqlExprIdentifier( SqlTokenIdentifier t )
            : base( t )
        {
        }

        public string Name { get { return Token.Name; } }

        public bool IsVariable { get { return Token.IsVariable; } }

        SqlTokenIdentifier ISqlIdentifier.IdentifierAt( int index )
        {
            if( index != 0 ) throw new ArgumentOutOfRangeException();
            return Token;
        }

        int ISqlIdentifier.IdentifiersCount
        {
            get { return 1; }
        }

        IEnumerable<SqlTokenIdentifier> ISqlIdentifier.Identifiers
        {
            get { return ItemsWithoutParenthesis.Cast<SqlTokenIdentifier>(); }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
