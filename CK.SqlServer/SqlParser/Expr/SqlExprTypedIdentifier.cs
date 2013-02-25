using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprTypedIdentifier : SqlExprBaseMonoToken
    {
        public SqlExprTypedIdentifier( SqlTokenIdentifier identifier, SqlExprTypeDecl type )
            : base( identifier )
        {
            if( identifier == null ) throw new ArgumentNullException( "identifier" );
            if( type == null ) throw new ArgumentNullException( "type" );

            Identifier = identifier;
            TypeDecl = type;
        }

        public SqlTokenIdentifier Identifier { get; private set; }

        public SqlExprTypeDecl TypeDecl { get; private set; }

        public override IEnumerable<SqlToken> Tokens
        {
            get { return base.Tokens.Concat( TypeDecl.Tokens ); }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }

}
