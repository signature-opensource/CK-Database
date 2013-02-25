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
    /// </summary>
    public class SqlExprStBlock : SqlExprBaseSt
    {
        readonly SqlTokenIdentifier _begin;
        readonly SqlExprStatementList _body;
        readonly SqlTokenIdentifier _end;

        public SqlExprStBlock( SqlTokenIdentifier begin, SqlExprStatementList body, SqlTokenIdentifier end, SqlTokenTerminal statementTerminator = null )
            : base( statementTerminator )
        {
            _begin = begin;
            _body = body;
            _end = end;
        }

        public SqlTokenIdentifier Begin { get { return _begin; } }

        public SqlExprStatementList Body { get { return _body; } }

        public SqlTokenIdentifier End { get { return _end; } }

        protected override IEnumerable<SqlToken> GetStatementTokens()
        {
            return new ReadOnlyListMono<SqlToken>( _begin ).Concat( _body.Tokens ).Concat( new ReadOnlyListMono<SqlToken>( _end ) );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
