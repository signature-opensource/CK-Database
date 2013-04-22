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
        public SqlExprAssign( ISqlIdentifier identifier, SqlTokenTerminal assignToken, SqlExpr right )
            : this( Build( identifier, assignToken, right ) )
        {
        }

        static ISqlItem[] Build( ISqlIdentifier identifier, SqlTokenTerminal assignToken, SqlExpr right )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( assignToken == null ) throw new ArgumentNullException( "assignToken" );
            if( right == null ) throw new ArgumentNullException( "right" );
            if( (assignToken.TokenType & SqlTokenType.IsAssignOperator) == 0 ) throw new ArgumentException( "Invalid assign token.", "assignToken" );
            return CreateArray( SqlToken.EmptyOpenPar, identifier, assignToken, right, SqlToken.EmptyClosePar );
        }

        internal SqlExprAssign( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public ISqlIdentifier Identifier { get { return (ISqlIdentifier)Slots[1]; } }

        public SqlTokenTerminal AssignToken { get { return (SqlTokenTerminal)Slots[2]; } }

        public SqlExpr Right { get { return (SqlExpr)Slots[3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
