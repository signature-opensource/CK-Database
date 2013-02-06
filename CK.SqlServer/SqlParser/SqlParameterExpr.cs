using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlParameterExpr : SqlExpr
    {
        public SqlParameterExpr( SourceLocation location, SqlTypedIdentifierExpr variable, bool isOutput, SqlLiteralExpr defaultValue )
            : base( location )
        {
            if( variable == null ) throw new ArgumentNullException( "variable" );
            if( !variable.Identifier.IsVariable ) throw new ArgumentException( "Must be a @VariableName", "variable" );
            Variable = variable;
            DefaultValue = defaultValue;
            IsOutput = isOutput;
        }

        public bool IsOutput { get; private set; }

        public SqlTypedIdentifierExpr Variable { get; private set; }

        public SqlLiteralExpr DefaultValue { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            string s = Variable.ToString();
            if( DefaultValue != null ) s += " = " + DefaultValue.LiteralValue;
            if( IsOutput ) s += " output";
            return s;
        }
    }

}
