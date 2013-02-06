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
    public class SqlTypeExpr : SqlExpr
    {
        public SqlTypeExpr( SourceLocation location, SqlDbType dbType, byte precision, byte scale )
            : base( location )
        {
            DbType = dbType;
            Size = 0;
            Precision = precision;
            Scale = scale;
        }

        public SqlTypeExpr( SourceLocation location, SqlDbType dbType, int size = 0 )
            : base( location )
        {
            DbType = dbType;
            Size = size;
            Precision = 0;
            Scale = 0;
        }

        public SqlTypeExpr( SourceLocation location, SqlDbType t, string userTypeName )
            : base( location )
        {
            DbType = t;
            UserTypeName = userTypeName;
            Size = 0;
            Precision = 0;
            Scale = 0;
        }

        public string UserTypeName { get; private set; }

        public SqlDbType DbType { get; private set; }

        public int Size { get; private set; }

        public byte Precision { get; private set; }
        
        public byte Scale { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            if( UserTypeName != null )
            {
                return UserTypeName;
            }
            if( Size > 0 )
            {
                return String.Format( "{0}({1})", DbType.ToString(), Size == -1 ? "max" : Size.ToString() );
            }
            if( Precision > 0 )
            {
                return String.Format( "{0}({1},{2})", DbType.ToString(), Precision, Size );
            }
            return DbType.ToString();
        }
    }

}
