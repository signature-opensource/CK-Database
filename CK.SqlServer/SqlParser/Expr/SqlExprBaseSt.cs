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
    /// Handles the mandatory statement terminator ';' that is required by ANSI SQL and future Sql Server versions.
    /// </summary>
    public abstract class SqlExprBaseSt : SqlExpr
    {
        readonly IAbstractExpr[] _components;
        readonly SqlTokenTerminal _stmtTerminator;

        protected SqlExprBaseSt( IList<IAbstractExpr> content, SqlTokenTerminal statementTerminator = null )
        {
            _stmtTerminator = statementTerminator;
            if( statementTerminator != null )
            {
                if( statementTerminator.TokenType != SqlTokenType.SemiColon ) throw new ArgumentException( "Statement terminator (;) expected.", "statementTerminator" );
                _components = CreateArray( content, content.Count, statementTerminator );
            }
            else _components = content is IAbstractExpr[] ? (IAbstractExpr[])content : content.ToArray();
        }

        protected SqlExprBaseSt( IAbstractExpr[] newContent )
        {
            _components = newContent;
            if( newContent.Length > 0 )
            {
                _stmtTerminator = newContent[newContent.Length - 1] as SqlTokenTerminal;
                if( _stmtTerminator.TokenType != SqlTokenType.SemiColon ) _stmtTerminator = null;
            }
        }

        protected int Count { get { return _components.Length; } }
        
        protected IAbstractExpr At( int i )
        {
            return _components[i];
        }

        public SqlTokenTerminal StatementTerminator
        {
            get { return _stmtTerminator; }
        }

        public IEnumerable<IAbstractExpr> ComponentsWithoutTerminator
        {
            get { return _stmtTerminator != null ? _components.Take( _components.Length - 1 ) : _components; }
        }

        public override sealed IEnumerable<IAbstractExpr> Components
        {
            get { return _components; }
        }
    }


}
