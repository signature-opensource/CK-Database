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
    /// Operator tokens.
    /// </summary>
    public abstract class SqlTokenTerminal : SqlToken
    {
        public SqlTokenTerminal( SqlTokenType t, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( (t & SqlTokenType.TerminalMask) == 0 ) throw new ArgumentException( "Invalid token type.", "t" );
        }

        protected override void DoWrite( StringBuilder b )
        {
            b.Append( SqlTokeniser.Explain( TokenType ) );
        }
    }

}
