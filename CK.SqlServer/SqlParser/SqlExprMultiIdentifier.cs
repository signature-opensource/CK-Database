using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprMultiIdentifier : SqlExprBaseListWithSeparatorList<SqlTokenIdentifier>
    {
        public SqlExprMultiIdentifier( IEnumerable<SqlToken> tokens )
            : base( tokens )
        {
        }

        public int IdentifierCount { get { return base.NonSeparatorCount; } }

        public IEnumerable<SqlTokenIdentifier> Identifiers { get { return base.NonSeparatorTokens; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
