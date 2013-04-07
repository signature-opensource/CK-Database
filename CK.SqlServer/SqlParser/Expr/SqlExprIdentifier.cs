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
    public class SqlExprIdentifier : SqlExprBaseMonoToken<SqlTokenIdentifier>, ISqlIdentifier
    {
        public SqlExprIdentifier( SqlTokenIdentifier t )
            : base( t )
        {
        }

        public string Name { get { return Token.Name; } }

        public bool IsVariable { get { return Token.IsVariable; } }

        public SqlTokenIdentifier this[ int index ]
        {
            get 
            { 
                if( index != 0 ) throw new ArgumentOutOfRangeException();
                return Token;
            }
        }

        int IReadOnlyCollection<SqlTokenIdentifier>.Count
        {
            get { return 1; }
        }

        IEnumerator<SqlTokenIdentifier> IEnumerable<SqlTokenIdentifier>.GetEnumerator()
        {
            return TypedTokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return TypedTokens.GetEnumerator();
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
