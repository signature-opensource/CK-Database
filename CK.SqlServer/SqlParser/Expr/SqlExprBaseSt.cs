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
    /// Base for all statements.
    /// Handles the mandatory statement terminator ';' that is required by ANSI sql and future Sql Server versions.
    /// </summary>
    public abstract class SqlExprBaseSt : SqlExpr
    {
        readonly SqlTokenTerminal _stmtTerminator;

        protected SqlExprBaseSt( SqlTokenTerminal statementTerminator = null )
        {
            _stmtTerminator = statementTerminator ?? SqlTokenTerminal.SemiColon;
        }

        public SqlTokenTerminal StatementTerminator 
        { 
            get { return _stmtTerminator; } 
        }

        public override sealed IEnumerable<SqlToken> Tokens
        {
            get { return GetStatementTokens().Concat( new ReadOnlyListMono<SqlToken>( _stmtTerminator ) ); }
        }

        protected abstract IEnumerable<SqlToken> GetStatementTokens();

    }


}
