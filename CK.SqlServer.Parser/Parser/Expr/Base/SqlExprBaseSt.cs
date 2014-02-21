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
    /// Base for all statements. It is a <see cref="SqlNoExpr"/> that handles the mandatory 
    /// statement terminator ';' that is required by ANSI SQL and future Sql Server versions.
    /// </summary>
    public abstract class SqlExprBaseSt : SqlNoExpr
    {
        readonly SqlTokenTerminal _stmtTerminator;

        protected SqlExprBaseSt( IList<ISqlItem> content, SqlTokenTerminal statementTerminator = null )
            : this( Build( content, statementTerminator ) )
        {
        }

        private static ISqlItem[] Build( IList<ISqlItem> content, SqlTokenTerminal statementTerminator )
        {
            if( statementTerminator != null )
            {
                if( statementTerminator.TokenType != SqlTokenType.SemiColon ) throw new ArgumentException( "Statement terminator (;) expected.", "statementTerminator" );
                return CreateArray( content, content.Count, statementTerminator );
            }
            return content.ToArray();
        }

        protected SqlExprBaseSt( ISqlItem[] items )
            : base( items )
        {
            _stmtTerminator = LastOrEmptyToken as SqlTokenTerminal;
            if( _stmtTerminator != null && _stmtTerminator.TokenType != SqlTokenType.SemiColon ) _stmtTerminator = null;
        }

        protected int SlotsLengthWithoutTerminator
        {
            get { return _stmtTerminator != null ? Slots.Length - 1 : Slots.Length; }
        }

        public SqlTokenTerminal StatementTerminator { get { return _stmtTerminator; } }

        public IEnumerable<ISqlItem> ComponentsWithoutTerminator
        {
            get { return _stmtTerminator != null ? Slots.Take( Slots.Length - 1 ) : Slots; }
        }
    }


}
