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
        readonly SqlTokenLiteral _assignToken;
        readonly SqlTokenIdentifier _outputClause;

        public SqlExprParameter( SqlExprTypedIdentifier declVar, SqlTokenTerminal assignToken = null, SqlToken defaultValue = null, SqlTokenIdentifier outputClause = null )
        {
            if( declVar == null ) throw new ArgumentNullException( "declVar" );
            if( !declVar.Identifier.IsVariable ) throw new ArgumentException( "Must be a @VariableName", "variable" );
            if( assignToken != null )
            {
                if( defaultValue == null || !SqlTokeniser.IsVariableNameOrLiteral( defaultValue.TokenType ) )
                {
                    throw new ArgumentException( "Must be a @VariableName or a literal.", "defaultValue" );
                }
            }
            else if( defaultValue != null )
            {
                throw new ArgumentNullException( "Assign token must be provided when a default value is specified.", "assignToken" );
            }
            if( outputClause != null 
                && (outputClause.IsUnquotedKeyword 
                        || (String.Compare( outputClause.Name, "out", StringComparison.OrdinalIgnoreCase ) != 0 
                            && String.Compare( outputClause.Name, "output", StringComparison.OrdinalIgnoreCase ) != 0 ) ) )
            {
                throw new ArgumentException( "Must be out or output.", "outputClause" );
            }
            Variable = declVar;
            DefaultValue = defaultValue;
            _outputClause = outputClause;
            IsOutput = _outputClause != null;
        }

        public SqlExprTypedIdentifier Variable { get; private set; }

        public SqlToken DefaultValue { get; private set; }

        public bool IsOutput { get; private set; }

        public override IEnumerable<SqlToken> Tokens
        {
            get 
            { 
                IEnumerable<SqlToken> t = Variable.Tokens;
                if( _assignToken != null ) 
                {
                    if( _outputClause != null )
                        t = t.Concat( new SqlToken[]{  _assignToken, DefaultValue, _outputClause } );
                    else t = t.Concat( new SqlToken[]{  _assignToken, DefaultValue } );
                }
                else if( _outputClause != null ) t = t.Concat( new SqlToken[]{ _outputClause } );
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
