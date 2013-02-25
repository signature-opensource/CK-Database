using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprStStoredProc : SqlExprBaseSt
    {
        readonly SqlTokenIdentifier     _alterOrCreate;
        readonly SqlTokenIdentifier     _type;
        readonly SqlExprMultiIdentifier _name;
        readonly SqlExprParameterList   _parameters;
        readonly SqlExprUnmodeledTokens _options;
        readonly SqlTokenIdentifier     _as;
        readonly SqlTokenIdentifier     _begin;
        readonly SqlExprStatementList   _bodyStatements;
        readonly SqlTokenIdentifier     _end;
        readonly IAbstractExpr[]        _all;

        public SqlExprStStoredProc( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprParameterList parameters, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlExprStatementList bodyStatements, SqlTokenTerminal term )
            : base( term )
        {
            _alterOrCreate = alterOrCreate;
            _type = type;
            _name = name;
            _parameters = parameters;
            _options = options;
            _as = asToken;
            _bodyStatements = bodyStatements;

            if( _options != null ) _all = new IAbstractExpr[] { _alterOrCreate, _type, _name, _parameters, _options, _as, _bodyStatements };
            else _all = new IAbstractExpr[] { _alterOrCreate, _type, _name, _parameters, _as, _bodyStatements };
        }

        public SqlExprStStoredProc( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprParameterList parameters, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlTokenIdentifier begin, SqlExprStatementList bodyStatements, SqlTokenIdentifier end, SqlTokenTerminal term )
            : base( term )
        {
            _alterOrCreate = alterOrCreate;
            _type = type;
            _name = name;
            _parameters = parameters;
            _options = options;
            _as = asToken;
            _begin = begin;
            _bodyStatements = bodyStatements;
            _end = end;

            if( _options != null ) _all = new IAbstractExpr[] { _alterOrCreate, _type, _name, _parameters, _options, _as, _begin, _bodyStatements, _end };
            else _all = new IAbstractExpr[] { _alterOrCreate, _type, _name, _parameters, _as, _begin, _bodyStatements, _end };
        }

        protected override IEnumerable<SqlToken> GetStatementTokens()
        {
            return Flatten( _all );
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
