using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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
            if( t.GetTypeInfo().IsEnum )
            {
                // This currently only works for enum with an underlying type
                // that is the Sql type (ie. enum => int or enum : byte => tinyint).
                //
                // TODO: Handle char, nchar and char(X), nchar(X) 
                // that must be mapped to the beginning of the enum names.
                Type uT = t.GetTypeInfo().GetEnumUnderlyingType();
                return uT == SqlHelper.FromSqlDbTypeToNetType( sql );
            }
            if( sql == SqlDbType.Char || sql == SqlDbType.NChar )
            {
                int sz = @this.SyntaxSize;
                if( sz == 0 || sz == 1 )
                {
                    if( t == typeof( char ) ) return true;
                }
                if( t == typeof( string ) ) return true;
            }
            else if( sql == SqlDbType.Udt )
            {
                string sqlSimpleTypeName = @this.ToStringClean();
                return (StringComparer.OrdinalIgnoreCase.Equals(sqlSimpleTypeName, "Geography") && t.Name == "SqlGeography")
                        || (StringComparer.OrdinalIgnoreCase.Equals(sqlSimpleTypeName, "Geometry") && t.Name == "SqlGeometry")
                        || (StringComparer.OrdinalIgnoreCase.Equals(sqlSimpleTypeName, "HierarchyId") && t.Name == "SqlHierarchyId");
            }
            else
            {
                Type tSql = SqlHelper.FromSqlDbTypeToNetType( sql );
                if( t == tSql ) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the implicit conversion method from a source to a target type.
        /// </summary>
        /// <param name="baseType">Source type.</param>
        /// <param name="targetType">Target type.</param>
        /// <returns>The method or null if not found.</returns>
        static MethodInfo GetImplicitConversionStaticMethod( Type baseType, Type targetType )
        {
            return baseType.GetMethods( BindingFlags.Public | BindingFlags.Static )
                            .FirstOrDefault( mi => {
                                if( mi.Name == "op_Implicit" && mi.ReturnType == targetType )
                                {
                                    ParameterInfo pi = mi.GetParameters().FirstOrDefault();
                                    return pi != null && pi.ParameterType == baseType;
                                }
                                return false;
                            } );
        }

        /// <summary>
        /// Attempts to get the best .Net type from this <see cref="ISqlServerUnifiedTypeDecl"/>.
        /// Null for <see cref="SqlDbType.Variant"/>.
        /// </summary>
        /// <param name="this">This ISqlServerUnifiedTypeDecl.</param>
        /// <returns>The best associated type.</returns>
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
