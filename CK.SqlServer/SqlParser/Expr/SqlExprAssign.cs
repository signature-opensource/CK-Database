using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprAssign : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprAssign( ISqlIdentifier identifier, SqlTokenTerminal assignToken, SqlExpr right )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( assignToken == null ) throw new ArgumentNullException( "assignToken" );
            if( right == null ) throw new ArgumentNullException( "right" );
            if( (assignToken.TokenType & SqlTokenType.IsAssignOperator) == 0 ) throw new ArgumentException( "Invalid assign token.", "assignToken" );
            _components = CreateArray( identifier, assignToken, right );
        }

        internal SqlExprAssign( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public ISqlIdentifier Identifier { get { return (ISqlIdentifier)_components[0]; } }

        public SqlTokenTerminal AssignToken { get { return (SqlTokenTerminal)_components[1]; } }

        public SqlExpr Right { get { return (SqlExpr)_components[2]; } }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
