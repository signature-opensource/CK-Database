using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlIdentifierExpr : SqlExpr
    {
        public SqlIdentifierExpr( SourceLocation location, SqlToken t, string name )
            : base( location )
        {
            if( String.IsNullOrWhiteSpace(name) ) throw new ArgumentNullException( "name" );
            if( t < 0 || (t & SqlToken.IsIdentifier) == 0 ) throw new ArgumentException( "Invalid identifier token.", "t" );
            Name = name;
            IdentifierType = t;
        }

        public string Name { get; private set; }

        public SqlToken IdentifierType { get; private set; }

        public bool IsVariable { get { return Name[0] == '@'; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            return Name;
        }
    }


}
