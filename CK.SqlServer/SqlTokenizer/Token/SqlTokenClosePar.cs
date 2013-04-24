using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
using System.Diagnostics;
using System.Globalization;

namespace CK.SqlServer
{
    /// <summary>
    /// Specific <see cref="SqlTokenTerminal"/> for <see cref="SqlTokenType.ClosePar"/>.
    /// </summary>
    public sealed class SqlTokenClosePar : SqlTokenTerminal 
    {
        public SqlTokenClosePar( IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( SqlTokenType.ClosePar, leadingTrivia, trailingTrivia )
        {
        }
    }

}
