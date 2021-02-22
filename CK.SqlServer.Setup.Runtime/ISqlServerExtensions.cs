using System;
using System.Data;
using System.Diagnostics;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Extends <see cref="ISqlServerUnifiedTypeDecl"/> sql description type.
    /// </summary>
    public static class ISqlServerExtensions
    {
        /// <summary>
        /// Gets whether a .Net type can be used as an input to this Sql Server type.
        /// <para>
        /// TODO: For the moment we just call <see cref="IsTypeCompatible"/>.
        /// </para>
        /// </summary>
        /// <param name="this">This Sql Server type.</param>
        /// <param name="t">.Net type to test.</param>
        /// <returns>True if the .Net type is compatible, false otherwise.</returns>
        static public bool IsContravariantTypeCompatible( this ISqlServerUnifiedTypeDecl @this, Type t )
        {
            return IsTypeCompatible( @this, t );
        }

        /// <summary>
        /// Gets whether a .Net type can be used as an output from this Sql Server type.
        /// <para>
        /// TODO: For the moment we just call <see cref="IsTypeCompatible"/>.
        /// </para>
        /// <para>
        /// TODO: To handle safe cast (short to int for instance), we must generate a chain of cast
        /// (a little bit like the one required by the enum). SafeAssignableCastChain (below) will do
        /// this job but work is needed here...
        /// </para>
        /// </summary>
        /// <param name="this">This Sql Server type.</param>
        /// <param name="t">.Net type to test.</param>
        /// <returns>True if the .Net type is compatible, false otherwise.</returns>
        static public bool IsCovariantTypeCompatible( this ISqlServerUnifiedTypeDecl @this, Type t )
        {
            return IsTypeCompatible( @this, t );
        }

        /// <summary>
        /// Gets whether a .Net type is compatible with this Sql Server type.
        /// </summary>
        /// <param name="this">This Sql Server type.</param>
        /// <param name="t">.Net type to test.</param>
        /// <returns>True if the .Net type is compatible, false otherwise.</returns>
        static public bool IsTypeCompatible( this ISqlServerUnifiedTypeDecl @this, Type t )
        {
            if( t.IsByRef ) t = t.GetElementType();
            Type underlyingType = Nullable.GetUnderlyingType( t );
            if( underlyingType != null ) t = underlyingType;
            SqlDbType sql = @this.DbType;
            if( t.IsEnum )
            {
                // This currently only works for enum with an underlying type
                // that is the Sql type (ie. enum => int or enum : byte => tinyint).
                //
                // TODO: Handle char, nchar and char(X), nchar(X) 
                // that must be mapped to the beginning of the enum names?
                Type uT = t.GetEnumUnderlyingType();
                Type tSql = SqlHelper.FromSqlDbTypeToNetType( sql );
                return uT == tSql;
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
        /// Gets a chain of types that must be used to cast an object of actual <paramref name="tSql"/> type
        /// into a value of the final <see cref="t"/> type.
        /// </summary>
        /// <param name="t">The final type.</param>
        /// <param name="tSql">The source type.</param>
        /// <returns>
        /// A chain of cast where the first one is the final <paramref name="t"/> type.
        /// Null if types are not compatible.
        /// </returns>
        //static (Type,Type?)? SafeAssignableCastChain( Type t, Type tSql )
        //{
        //    Debug.Assert( Convert.ToInt32( (decimal)Int32.MaxValue ) == Int32.MaxValue );
        //    Debug.Assert( Convert.ToInt64( (decimal)Int64.MaxValue ) == Int64.MaxValue );

        //    if( t == tSql )
        //    {
        //        return (t, null);
        //    }
        //    if( )
        //    if( t == typeof( Int16 ) )
        //    {
        //        if( tSql == typeof( SByte ) ) return (t, tSql);
        //        if( tSql == typeof( Byte ) ) return (t,tSql);
        //    }
        //    else if( t == typeof( UInt16 ) )
        //    {
        //        // Normally, Char can be implicitly converted but we disallow this here: only Byte can be safely converted into ushort.
        //        return tSql == typeof( Byte );
        //    }
        //    else if( t == typeof( Int32 ) )
        //    {
        //        // Normally, Char can be implicitly converted but we disallow this here: only Byte can be safely converted into ushort.
        //        return tSql == typeof( SByte ) || tSql == typeof( Byte )
        //            || tSql == typeof( Int16 ) || tSql == typeof( UInt16 );
        //    }
        //    else if( t == typeof( UInt32 ) )
        //    {
        //        return tSql == typeof( Byte ) || tSql == typeof( UInt16 );
        //    }
        //    else if( t == typeof( Int64 ) )
        //    {
        //        return tSql == typeof( SByte ) || tSql == typeof( Byte )
        //            || tSql == typeof( Int16 ) || tSql == typeof( UInt16 )
        //            || tSql == typeof( Int32 ) || tSql == typeof( UInt32 );
        //    }
        //    else if( t == typeof( UInt64 ) )
        //    {
        //        return tSql == typeof( Byte ) || tSql == typeof( UInt16 ) || tSql == typeof( UInt32 );
        //    }
        //    else if( t == typeof( Single ) || t == typeof( Double ) )
        //    {
        //        Debug.Assert( Convert.ToInt32( (double)int.MaxValue ) != int.MaxValue );
        //        // Integers stored in Single and even in a Double lose digits. 
        //        return tSql == typeof( SByte ) || tSql == typeof( Byte )
        //            || tSql == typeof( Int16 ) || tSql == typeof( UInt16 )
        //            || tSql == typeof( Single );
        //    }
        //    else if( t == typeof( Int16 ) )
        //    {
        //        return tSql == typeof( Byte ) || tSql == typeof( SByte );
        //    }
        //    else if( t == typeof( Decimal ) )
        //    {
        //        return tSql == typeof( Byte ) || tSql == typeof( SByte )
        //            || tSql == typeof( Int16 ) || tSql == typeof( UInt16 )
        //            || tSql == typeof( Int32 ) || tSql == typeof( UInt32 )
        //            || tSql == typeof( Int64 ) || tSql == typeof( UInt64 );
        //    }
        //    return false;
        //}
    }
}
