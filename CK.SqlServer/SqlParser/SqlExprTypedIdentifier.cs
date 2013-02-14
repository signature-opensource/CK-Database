using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprTypedIdentifier : SqlExpr
    {
        public SqlExprTypedIdentifier( SqlExprIdentifier identifier, SqlExprType type )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( type == null ) throw new ArgumentNullException( "type" );

            Identifier = identifier;
            SqlType = type;
        }

        public SqlExprIdentifier Identifier { get; private set; }

        public SqlExprType SqlType { get; private set; }

        public override IEnumerable<SqlToken> Tokens
        {
            get { return Identifier.Tokens.Concat( SqlType.Tokens ); }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
