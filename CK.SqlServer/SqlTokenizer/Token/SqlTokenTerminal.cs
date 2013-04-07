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
    /// Covers operators, punctuations and brakets: the token is fully defined by its <see cref="SqlToken.TokenType"/> itself (no associated value is necessary).
    /// </summary>
    public class SqlTokenTerminal : SqlToken
    {
        public static readonly SqlTokenTerminal Dot = new SqlTokenTerminal( SqlTokenType.Dot, null, null );
        public static readonly SqlTokenTerminal Comma = new SqlTokenTerminal( SqlTokenType.Comma, null, null );
        public static readonly SqlTokenTerminal SemiColon = new SqlTokenTerminal( SqlTokenType.SemiColon, null, null );
        public static readonly SqlTokenTerminal OpenPar = new SqlTokenOpenPar( null, null );
        public static readonly SqlTokenTerminal ClosePar = new SqlTokenClosePar( null, null );

        public SqlTokenTerminal( SqlTokenType t, IReadOnlyList<SqlTrivia> leadingTrivia = null, IReadOnlyList<SqlTrivia> trailingTrivia = null )
            : base( t, leadingTrivia, trailingTrivia )
        {
            if( (t & SqlTokenType.TerminalMask) == 0 ) throw new ArgumentException( "Invalid token type.", "t" );
        }

        protected override void DoWrite( StringBuilder b )
        {
            b.Append( SqlTokenizer.Explain( TokenType ) );
        }
    }

}
