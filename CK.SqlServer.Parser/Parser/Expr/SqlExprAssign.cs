using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer.Parser
{
    public class SqlExprAssign : SqlExpr
    {
        public SqlExprAssign( ISqlIdentifier identifier, SqlTokenTerminal assignTok, SqlExpr right )
            : this( Build( identifier, assignTok, right ) )
        {
        }

        static ISqlItem[] Build( ISqlIdentifier identifier, SqlTokenTerminal assignTok, SqlExpr right )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( assignTok == null ) throw new ArgumentNullException( "assignTok" );
            if( right == null ) throw new ArgumentNullException( "right" );
            if( (assignTok.TokenType & SqlTokenType.IsAssignOperator) == 0 ) throw new ArgumentException( "Invalid assign token.", "assignTok" );
            return CreateArray( SqlToken.EmptyOpenPar, identifier, assignTok, right, SqlToken.EmptyClosePar );
        }

        internal SqlExprAssign( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        public ISqlIdentifier Identifier { get { return (ISqlIdentifier)Slots[1]; } }

        public SqlTokenTerminal AssignTok { get { return (SqlTokenTerminal)Slots[2]; } }

        public SqlExpr Right { get { return (SqlExpr)Slots[3]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }
}
