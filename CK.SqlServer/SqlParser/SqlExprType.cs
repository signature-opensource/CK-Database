using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    public class SqlExprType : SqlExpr
    {
        readonly ISqlExprUnifiedType _type;

        public SqlExprType( ISqlExprUnifiedType actualType )
        {
            if( actualType == null ) throw new ArgumentNullException( "actualType" );
            _type = actualType;
        }

        public override IEnumerable<SqlToken> Tokens { get { return _type.Tokens; } }

        public ISqlExprUnifiedType ActualType 
        {
            get { return _type; } 
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
