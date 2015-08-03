using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Parser
{
    public static class ISqlServerExtensions
    {
        static public bool IsTypeCompatible( this ISqlServerUnifiedTypeDecl @this, Type t )
        {
            if( t.IsByRef ) t = t.GetElementType();
            Type underlyingType = Nullable.GetUnderlyingType( t );
            if( underlyingType != null ) t = underlyingType;
            SqlDbType sql = @this.DbType;
            if( sql == SqlDbType.Char || sql == SqlDbType.NChar )
            {
                int sz = @this.SyntaxSize;
                if( sz == 0 || sz == 1 )
                {
                    if( t == typeof( char ) ) return true;
                }
                if( t == typeof( string ) ) return true;
            }
            else
            {
                if( t == SqlHelper.FromSqlDbTypeToNetType( sql ) ) return true;
            }
            return false;
        }

        static public Type BestNetType( this ISqlServerUnifiedTypeDecl @this )
        {
            SqlDbType sql = @this.DbType;
            if( sql == SqlDbType.Char || sql == SqlDbType.NChar )
            {
                int sz = @this.SyntaxSize;
                if( sz == 0 || sz == 1 )
                {
                    return typeof( char );
                }
                return typeof( string );
            }
            return SqlHelper.FromSqlDbTypeToNetType( sql );
        }


    }
}
