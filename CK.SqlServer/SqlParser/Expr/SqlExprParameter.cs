using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprParameter : SqlExpr
    {
        readonly SqlTokenIdentifier _outputClause;
        readonly SqlTokenIdentifier _readonlyClause;

        public SqlExprParameter( SqlExprTypedIdentifier declVar, SqlExprParameterDefaultValue defaultValue = null, SqlTokenIdentifier outputClause = null, SqlTokenIdentifier readonlyClause = null )
        {
            if( declVar == null ) throw new ArgumentNullException( "declVar" );
            if( !declVar.Identifier.IsVariable ) throw new ArgumentException( "Must be a @VariableName", "variable" );
            if( outputClause != null
                && (!outputClause.IsUnquotedKeyword
                        || (String.Compare( outputClause.Name, "out", StringComparison.OrdinalIgnoreCase ) != 0
                            && String.Compare( outputClause.Name, "output", StringComparison.OrdinalIgnoreCase ) != 0)) )
            {
                throw new ArgumentException( "Must be out or output.", "outputClause" );
            }
            if( readonlyClause != null
                && (!readonlyClause.IsUnquotedKeyword || String.Compare( outputClause.Name, "readonly", StringComparison.OrdinalIgnoreCase ) != 0 ) )
            {
                throw new ArgumentException( "Must be readonly.", "readonlyClause" );
            }
            Variable = declVar;
            DefaultValue = defaultValue;
            _outputClause = outputClause;
            _readonlyClause = readonlyClause;
        }

        public SqlExprTypedIdentifier Variable { get; private set; }

        public SqlExprParameterDefaultValue DefaultValue { get; private set; }

        public bool IsOutput { get { return _outputClause != null; } }

        public bool IsreadOnly { get { return _readonlyClause != null; } }

        public override IEnumerable<SqlToken> Tokens
        {
            get 
            {
                var t = Variable.Tokens;
                if( DefaultValue != null ) t = t.Concat( DefaultValue.Tokens );
                if( _outputClause != null ) t = t.Concat( new ReadOnlyListMono<SqlToken>( _outputClause ) );
                return t;
            }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
